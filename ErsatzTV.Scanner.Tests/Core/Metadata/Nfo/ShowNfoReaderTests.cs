﻿using System.Text;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Scanner.Core.Metadata.Nfo;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Scanner.Tests.Core.Metadata.Nfo;

[TestFixture]
public class ShowNfoReaderTests
{
    [SetUp]
    public void SetUp() => _showNfoReader = new ShowNfoReader(
        new RecyclableMemoryStreamManager(),
        Substitute.For<IClient>(),
        new NullLogger<ShowNfoReader>());

    private ShowNfoReader _showNfoReader;

    [Test]
    public async Task ParsingNfo_Should_Return_Error()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsLeft.ShouldBeTrue();
    }

    [Test]
    public async Task MetadataNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"<tvshow></tvshow>"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsRight.ShouldBeTrue();
    }

    [Test]
    public async Task CombinationNfo_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<tvshow></tvshow>
https://www.themoviedb.org/movie/11-star-wars"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsRight.ShouldBeTrue();
    }

    [Test]
    public async Task FullSample_Should_Return_Nfo()
    {
        // nested "title" in "seasondetails" may cause the last encountered title to be used for the show
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes"" ?>
<tvshow>
    <title>WandaVision</title>
    <originaltitle>WandaVision</originaltitle>
    <showtitle>WandaVision</showtitle>
    <ratings>
        <rating name=""imdb"" max=""10"" default=""true"">
            <value>8.200000</value>
            <votes>105359</votes>
        </rating>
        <rating name=""tmdb"" max=""10"">
            <value>8.500000</value>
            <votes>7230</votes>
        </rating>
        <rating name=""trakt"" max=""10"">
            <value>8.077950</value>
            <votes>3284</votes>
        </rating>
    </ratings>
    <userrating>0</userrating>
    <top250>0</top250>
    <season>1</season>
    <episode>9</episode>
    <displayseason>-1</displayseason>
    <displayepisode>-1</displayepisode>
    <outline></outline>
    <plot>Wanda Maximoff and Vision—two super-powered beings living idealized suburban lives—begin to suspect that everything is not as it seems.</plot>
    <tagline></tagline>
    <runtime>0</runtime>
    <thumb spoof="""" cache="""" aspect=""landscape"" preview=""https://image.tmdb.org/t/p/w780/dUWto4NaeJFrGx7jm8m3KLymUGf.jpg"">https://image.tmdb.org/t/p/original/dUWto4NaeJFrGx7jm8m3KLymUGf.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""poster"" preview=""https://image.tmdb.org/t/p/w780/8UsAB1hgwnd80eI2ociyppB6UXL.jpg"">https://image.tmdb.org/t/p/original/8UsAB1hgwnd80eI2ociyppB6UXL.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""poster"" preview=""https://assets.fanart.tv/preview/tv/362392/tvposter/wandavision-6009571d1ed1f.jpg"">https://assets.fanart.tv/fanart/tv/362392/tvposter/wandavision-6009571d1ed1f.jpg</thumb>
    <thumb spoof="""" cache="""" aspect=""clearlogo"" preview=""https://assets.fanart.tv/preview/tv/362392/hdtvlogo/marvels-wandavision-5f6ac3b1e9458.png"">https://assets.fanart.tv/fanart/tv/362392/hdtvlogo/marvels-wandavision-5f6ac3b1e9458.png</thumb>
    <thumb spoof="""" cache="""" aspect=""clearart"" preview=""https://assets.fanart.tv/preview/tv/362392/hdclearart/wandavision-6009b6875a285.png"">https://assets.fanart.tv/fanart/tv/362392/hdclearart/wandavision-6009b6875a285.png</thumb>
    <thumb spoof="""" cache="""" aspect=""landscape"" preview=""https://assets.fanart.tv/preview/tv/362392/tvthumb/wandavision-603032a5349b9.jpg"">https://assets.fanart.tv/fanart/tv/362392/tvthumb/wandavision-603032a5349b9.jpg</thumb>
    <thumb spoof="""" cache="""" season=""1"" type=""season"" aspect=""poster"" preview=""https://image.tmdb.org/t/p/w780/7u443QI5xNIfLgNzEsV43CYZCWX.jpg"">https://image.tmdb.org/t/p/original/7u443QI5xNIfLgNzEsV43CYZCWX.jpg</thumb>
    <fanart>
        <thumb colors="""" preview="""">https://image.tmdb.org/t/p/original/57vVjteucIF3bGnZj6PmaoJRScw.jpg</thumb>
        <thumb colors="""" preview="""">https://assets.fanart.tv/fanart/tv/362392/showbackground/marvels-wandavision-5ff4fef387a43.jpg</thumb>
    </fanart>
    <mpaa>Australia:M</mpaa>
    <playcount>0</playcount>
    <lastplayed>2021-03-29</lastplayed>
    <id>85271</id>
    <uniqueid type=""imdb"">tt9140560</uniqueid>
    <uniqueid type=""tmdb"" default=""true"">85271</uniqueid>
    <uniqueid type=""tvdb"">362392</uniqueid>
    <genre>SuperHero</genre>
    <premiered>2021-01-15</premiered>
    <year>2021</year>
    <status>Ended</status>
    <code></code>
    <aired></aired>
    <studio>Disney+</studio>
    <actor>
        <name>Elizabeth Olsen</name>
        <role>Wanda Maximoff / The Scarlet Witch</role>
        <order>0</order>
        <thumb>https://image.tmdb.org/t/p/original/wIU675y4dofIDVuhaNWPizJNtep.jpg</thumb>
    </actor>
    <actor>
        <name>Paul Bettany</name>
        <role>Vision / The Vision</role>
        <order>1</order>
        <thumb>https://image.tmdb.org/t/p/original/vcAVrAOZrpqmi37qjFdztRAv1u9.jpg</thumb>
    </actor>
    <namedseason number=""1"">Season 1</namedseason>
    <resume>
        <position>0.000000</position>
        <total>0.000000</total>
    </resume>
    <dateadded>2021-03-12 06:15:51</dateadded>
    <seasons>
      <seasondetails>
        <season>-1</season>
        <title>* All Seasons</title>
        <locked>false</locked>
      </seasondetails>
      <seasondetails>
        <season>1</season>
        <tvdb>1824025</tvdb>
        <locked>false</locked>
      </seasondetails>
      <seasondetails>
        <season>2</season>
        <title>Season 02</title>
        <tvdb>1993428</tvdb>
        <locked>false</locked>
      </seasondetails>
    </seasons>
</tvshow>"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsRight.ShouldBeTrue();

        foreach (ShowNfo nfo in result.RightToSeq())
        {
            nfo.Title.ShouldBe("WandaVision");
            nfo.Year.ShouldBe(2021);
            nfo.Plot.ShouldBe(
                "Wanda Maximoff and Vision—two super-powered beings living idealized suburban lives—begin to suspect that everything is not as it seems.");
            nfo.ContentRating.ShouldBe("Australia:M");
            nfo.Genres.ShouldBeEquivalentTo(new List<string> { "SuperHero" });
            nfo.Studios.ShouldBeEquivalentTo(new List<string> { "Disney+" });
            nfo.Actors.ShouldBeEquivalentTo(
                new List<ActorNfo>
                {
                    new()
                    {
                        Name = "Elizabeth Olsen", Order = 0, Role = "Wanda Maximoff / The Scarlet Witch",
                        Thumb = "https://image.tmdb.org/t/p/original/wIU675y4dofIDVuhaNWPizJNtep.jpg"
                    },
                    new()
                    {
                        Name = "Paul Bettany", Order = 1, Role = "Vision / The Vision",
                        Thumb = "https://image.tmdb.org/t/p/original/vcAVrAOZrpqmi37qjFdztRAv1u9.jpg"
                    }
                });
            nfo.UniqueIds.ShouldBeEquivalentTo(
                new List<UniqueIdNfo>
                {
                    new() { Type = "imdb", Guid = "tt9140560", Default = false },
                    new() { Type = "tmdb", Guid = "85271", Default = true },
                    new() { Type = "tvdb", Guid = "362392", Default = false }
                });
            nfo.Premiered.IsSome.ShouldBeTrue();
            foreach (DateTime premiered in nfo.Premiered)
            {
                premiered.ShouldBe(new DateTime(2021, 1, 15));
            }
        }
    }

    [Test]
    public async Task MetadataNfo_With_Outline_Should_Return_Nfo()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"<tvshow><outline>Test Outline</outline></tvshow>"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsRight.ShouldBeTrue();
        foreach (ShowNfo nfo in result.RightToSeq())
        {
            nfo.Outline.ShouldBe("Test Outline");
        }
    }

    [Test]
    public async Task MetadataNfo_With_Tagline_Should_Return_Nfo()
    {
        await using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes(@"<tvshow><tagline>Test Tagline</tagline></tvshow>"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsRight.ShouldBeTrue();
        foreach (ShowNfo nfo in result.RightToSeq())
        {
            nfo.Tagline.ShouldBe("Test Tagline");
        }
    }

    [Test]
    public async Task MetadataNfo_With_Tag_Should_Return_Nfo()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"<tvshow><tag>Test Tag</tag></tvshow>"));

        Either<BaseError, ShowNfo> result = await _showNfoReader.Read(stream);

        result.IsRight.ShouldBeTrue();
        foreach (ShowNfo nfo in result.RightToSeq())
        {
            nfo.Tags.ShouldBeEquivalentTo(new List<string> { "Test Tag" });
        }
    }
}
