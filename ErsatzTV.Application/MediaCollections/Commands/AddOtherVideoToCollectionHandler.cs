﻿using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class AddOtherVideoToCollectionHandler :
    IRequestHandler<AddOtherVideoToCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _searchChannel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;

    public AddOtherVideoToCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> searchChannel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
        _searchChannel = searchChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        AddOtherVideoToCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(
            validation,
            parameters => ApplyAddOtherVideoRequest(dbContext, parameters));
    }

    private async Task<Unit> ApplyAddOtherVideoRequest(TvContext dbContext, Parameters parameters)
    {
        parameters.Collection.MediaItems.Add(parameters.OtherVideo);
        if (await dbContext.SaveChangesAsync() > 0)
        {
            await _searchChannel.WriteAsync(new ReindexMediaItems([parameters.OtherVideo.Id]));

            // refresh all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository
                         .PlayoutIdsUsingCollection(parameters.Collection.Id))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        AddOtherVideoToCollection request) =>
        (await CollectionMustExist(dbContext, request), await ValidateOtherVideo(dbContext, request))
        .Apply((collection, episode) => new Parameters(collection, episode));

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        AddOtherVideoToCollection request) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private static Task<Validation<BaseError, OtherVideo>> ValidateOtherVideo(
        TvContext dbContext,
        AddOtherVideoToCollection request) =>
        dbContext.OtherVideos
            .SelectOneAsync(m => m.Id, e => e.Id == request.OtherVideoId)
            .Map(o => o.ToValidation<BaseError>("OtherVideo does not exist"));

    private sealed record Parameters(Collection Collection, OtherVideo OtherVideo);
}
