﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling;
using Shouldly;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class PlayoutModeSchedulerFloodTests : SchedulerTestBase
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private CancellationToken _cancellationToken;

    [Test]
    public void Should_Fill_Exactly_To_Next_Schedule_Item()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null,
            CustomTitle = "CustomTitle"
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(2); // one guide group here because of custom title
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(3);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[0].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.ShouldBe(1);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[1].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.ShouldBe(1);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[2].CustomTitle.ShouldBe("CustomTitle");
    }

    [Test]
    public void Should_Schedule_Single_Item_Fixed_Start_Flood()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = TimeSpan.Zero,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null,
            CustomTitle = "CustomTitle"
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            scheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(6));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(2); // one guide group here because of custom title
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeTrue();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(0);

        enumerator.State.Index.ShouldBe(0);

        playoutItems.Count.ShouldBe(6);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[0].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.ShouldBe(1);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[1].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.ShouldBe(1);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[2].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[3].MediaItemId.ShouldBe(2);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems[3].GuideGroup.ShouldBe(1);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[3].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[4].MediaItemId.ShouldBe(1);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.AddHours(4));
        playoutItems[4].GuideGroup.ShouldBe(1);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[4].CustomTitle.ShouldBe("CustomTitle");

        playoutItems[5].MediaItemId.ShouldBe(2);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.AddHours(5));
        playoutItems[5].GuideGroup.ShouldBe(1);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.None);
        playoutItems[5].CustomTitle.ShouldBe("CustomTitle");
    }

    [Test]
    public void Should_Fill_Exactly_To_Next_Schedule_Item_Flood()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            // this caused trouble with the peek logic and the IsFlood flag
            new ProgramScheduleItemFlood
            {
                StartTime = TimeSpan.FromHours(3)
            }
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(3);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);
    }

    [Test]
    public void Should_Fill_Exactly_To_Next_Schedule_Item_With_Post_Roll_Multiple_One()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            PostRollFiller = new FillerPreset
            {
                FillerKind = FillerKind.PostRoll,
                FillerMode = FillerMode.Count,
                Count = 1,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            TailFiller = null,
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.PostRollFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator1.State.Index.ShouldBe(1);
        enumerator2.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(6);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(3);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.ShouldBe(1);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.PostRoll);

        playoutItems[2].MediaItemId.ShouldBe(2);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddHours(1));
        playoutItems[2].GuideGroup.ShouldBe(2);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(4);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(1, 55, 0)));
        playoutItems[3].GuideGroup.ShouldBe(2);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.PostRoll);

        playoutItems[4].MediaItemId.ShouldBe(1);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems[4].GuideGroup.ShouldBe(3);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[5].MediaItemId.ShouldBe(3);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
        playoutItems[5].GuideGroup.ShouldBe(3);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.PostRoll);
    }

    [Test]
    public void Should_Have_Gap_With_No_Tail_No_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(3);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Exact_Tail()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator1.State.Index.ShouldBe(1);
        enumerator2.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(6);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(3);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.ShouldBe(3);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[4].MediaItemId.ShouldBe(4);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 50, 0)));
        playoutItems[4].GuideGroup.ShouldBe(3);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[5].MediaItemId.ShouldBe(3);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 55, 0)));
        playoutItems[5].GuideGroup.ShouldBe(3);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.Tail);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(5));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            }
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.FallbackFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator1.State.Index.ShouldBe(1);
        enumerator2.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(4);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(3);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.ShouldBe(3);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.Fallback);
    }

    [Test]
    public void Should_Have_Gap_With_Tail_No_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator1, scheduleItem.TailFiller, enumerator2),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator1.State.Index.ShouldBe(1);
        enumerator2.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(6);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(3);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.ShouldBe(3);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[4].MediaItemId.ShouldBe(4);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
        playoutItems[4].GuideGroup.ShouldBe(3);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[5].MediaItemId.ShouldBe(3);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
        playoutItems[5].GuideGroup.ShouldBe(3);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.Tail);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Tail_And_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
        Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Fallback,
                Collection = collectionThree,
                CollectionId = collectionThree.Id
            }
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var enumerator3 = new ChronologicalMediaCollectionEnumerator(
            collectionThree.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(
                scheduleItem,
                enumerator1,
                scheduleItem.TailFiller,
                enumerator2,
                scheduleItem.FallbackFiller,
                enumerator3),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator1.State.Index.ShouldBe(1);
        enumerator2.State.Index.ShouldBe(1);
        enumerator3.State.Index.ShouldBe(1);

        playoutItems.Count.ShouldBe(7);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(55));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(1, 50, 0)));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(3);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 45, 0)));
        playoutItems[3].GuideGroup.ShouldBe(3);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[4].MediaItemId.ShouldBe(4);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 49, 0)));
        playoutItems[4].GuideGroup.ShouldBe(3);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[5].MediaItemId.ShouldBe(3);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 53, 0)));
        playoutItems[5].GuideGroup.ShouldBe(3);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.Tail);

        playoutItems[6].MediaItemId.ShouldBe(5);
        playoutItems[6].StartOffset.ShouldBe(startState.CurrentTime.Add(new TimeSpan(2, 57, 0)));
        playoutItems[6].GuideGroup.ShouldBe(3);
        playoutItems[6].FillerKind.ShouldBe(FillerKind.Fallback);
    }

    [Test]
    public void Should_Not_Schedule_Fallback_Filler_Incomplete_Flood()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(20));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Fallback,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            }
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());

        // hard stop at 2, an hour before the "next schedule item" at 3
        DateTimeOffset hardStop = StartState(scheduleItemsEnumerator).CurrentTime.AddHours(2);

        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(
                scheduleItem,
                enumerator1,
                scheduleItem.FallbackFiller,
                enumerator2),
            scheduleItem,
            NextScheduleItem,
            hardStop,
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(7);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeTrue();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(0);

        enumerator1.State.Index.ShouldBe(0);
        enumerator2.State.Index.ShouldBe(0);

        playoutItems.Count.ShouldBe(6);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(20));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(40));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(2);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(60));
        playoutItems[3].GuideGroup.ShouldBe(4);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[4].MediaItemId.ShouldBe(1);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(80));
        playoutItems[4].GuideGroup.ShouldBe(5);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[5].MediaItemId.ShouldBe(2);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(100));
        playoutItems[5].GuideGroup.ShouldBe(6);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.None);
    }

    [Test]
    public void Should_Not_Schedule_Tail_Filler_Incomplete_Flood()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(20));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = null
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());

        // hard stop at 2, an hour before the "next schedule item" at 3
        DateTimeOffset hardStop = StartState(scheduleItemsEnumerator).CurrentTime.AddHours(2);

        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(
                scheduleItem,
                enumerator1,
                scheduleItem.TailFiller,
                enumerator2),
            scheduleItem,
            NextScheduleItem,
            hardStop,
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(7);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeTrue();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(0);

        enumerator1.State.Index.ShouldBe(0);
        enumerator2.State.Index.ShouldBe(0);

        playoutItems.Count.ShouldBe(6);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(20));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(40));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[3].MediaItemId.ShouldBe(2);
        playoutItems[3].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(60));
        playoutItems[3].GuideGroup.ShouldBe(4);
        playoutItems[3].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[4].MediaItemId.ShouldBe(1);
        playoutItems[4].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(80));
        playoutItems[4].GuideGroup.ShouldBe(5);
        playoutItems[4].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[5].MediaItemId.ShouldBe(2);
        playoutItems[5].StartOffset.ShouldBe(startState.CurrentTime.AddMinutes(100));
        playoutItems[5].GuideGroup.ShouldBe(6);
        playoutItems[5].FillerKind.ShouldBe(FillerKind.None);
    }

    [Test]
    public void Should_Not_Have_Gap_With_Unused_Tail_And_Unused_Fallback()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromHours(1));
        Collection collectionTwo = TwoItemCollection(3, 4, TimeSpan.FromMinutes(4));
        Collection collectionThree = TwoItemCollection(5, 6, TimeSpan.FromMinutes(1));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = null,
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = new FillerPreset
            {
                FillerKind = FillerKind.Tail,
                Collection = collectionTwo,
                CollectionId = collectionTwo.Id
            },
            FallbackFiller = new FillerPreset
            {
                FillerKind = FillerKind.Fallback,
                Collection = collectionThree,
                CollectionId = collectionThree.Id
            }
        };

        var enumerator1 = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var enumerator2 = new ChronologicalMediaCollectionEnumerator(
            collectionTwo.MediaItems,
            new CollectionEnumeratorState());

        var enumerator3 = new ChronologicalMediaCollectionEnumerator(
            collectionThree.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(
                scheduleItem,
                enumerator1,
                scheduleItem.TailFiller,
                enumerator2,
                scheduleItem.FallbackFiller,
                enumerator3),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutBuilderState.CurrentTime.ShouldBe(startState.CurrentTime.AddHours(3));
        playoutItems.Last().FinishOffset.ShouldBe(playoutBuilderState.CurrentTime);

        playoutBuilderState.NextGuideGroup.ShouldBe(4);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(1);

        enumerator1.State.Index.ShouldBe(1);
        enumerator2.State.Index.ShouldBe(0);
        enumerator3.State.Index.ShouldBe(0);

        playoutItems.Count.ShouldBe(3);

        playoutItems[0].MediaItemId.ShouldBe(1);
        playoutItems[0].StartOffset.ShouldBe(startState.CurrentTime);
        playoutItems[0].GuideGroup.ShouldBe(1);
        playoutItems[0].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[1].MediaItemId.ShouldBe(2);
        playoutItems[1].StartOffset.ShouldBe(startState.CurrentTime.AddHours(1));
        playoutItems[1].GuideGroup.ShouldBe(2);
        playoutItems[1].FillerKind.ShouldBe(FillerKind.None);

        playoutItems[2].MediaItemId.ShouldBe(1);
        playoutItems[2].StartOffset.ShouldBe(startState.CurrentTime.AddHours(2));
        playoutItems[2].GuideGroup.ShouldBe(3);
        playoutItems[2].FillerKind.ShouldBe(FillerKind.None);
    }

    [Test]
    public void Should_Not_Schedule_At_HardStop()
    {
        Collection collectionOne = TwoItemCollection(1, 2, TimeSpan.FromMinutes(55));

        var scheduleItem = new ProgramScheduleItemFlood
        {
            Id = 1,
            Index = 1,
            Collection = collectionOne,
            CollectionId = collectionOne.Id,
            StartTime = TimeSpan.FromHours(6),
            PlaybackOrder = PlaybackOrder.Chronological,
            TailFiller = null,
            FallbackFiller = null
        };

        var enumerator = new ChronologicalMediaCollectionEnumerator(
            collectionOne.MediaItems,
            new CollectionEnumeratorState());

        var sortedScheduleItems = new List<ProgramScheduleItem>
        {
            scheduleItem,
            NextScheduleItem
        };

        var scheduleItemsEnumerator = new OrderedScheduleItemsEnumerator(
            sortedScheduleItems,
            new CollectionEnumeratorState());

        PlayoutBuilderState startState = StartState(scheduleItemsEnumerator);

        var scheduler = new PlayoutModeSchedulerFlood(Substitute.For<ILogger>());
        (PlayoutBuilderState playoutBuilderState, List<PlayoutItem> playoutItems) = scheduler.Schedule(
            startState,
            CollectionEnumerators(scheduleItem, enumerator),
            scheduleItem,
            NextScheduleItem,
            HardStop(scheduleItemsEnumerator),
            _cancellationToken);

        playoutItems.ShouldBeEmpty();

        playoutBuilderState.CurrentTime.ShouldBe(HardStop(scheduleItemsEnumerator));

        playoutBuilderState.NextGuideGroup.ShouldBe(1);
        playoutBuilderState.DurationFinish.IsNone.ShouldBeTrue();
        playoutBuilderState.InFlood.ShouldBeFalse();
        playoutBuilderState.MultipleRemaining.IsNone.ShouldBeTrue();
        playoutBuilderState.InDurationFiller.ShouldBeFalse();
        playoutBuilderState.ScheduleItemsEnumerator.State.Index.ShouldBe(0);

        enumerator.State.Index.ShouldBe(0);
    }

    protected override ProgramScheduleItem NextScheduleItem => new ProgramScheduleItemOne
    {
        StartTime = TimeSpan.FromHours(3)
    };
}
