// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Train
{
    public interface IModelTrainerSettings : ITrainingSettings
    {
        /// <summary>
        /// Indicates how to group usage events before counting co-occurrences. 
        /// A 'User' co-occurrence unit will consider all items purchased by the same user as occurring together in the same session. 
        /// A 'Timestamp' co-occurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session.
        /// </summary>
        CooccurrenceUnit CooccurrenceUnit { get; }

        /// <summary>
        /// For user-to-item recommendations, it defines whether the event type and the time of the event should be considered as 
        /// input into the scoring. 
        /// </summary>
        bool EnableUserAffinity { get; }

        /// <summary>
        /// Enables user to item recommendations by storing the usage events per user and using it for recommendations.
        /// Setting this to true will impact the performance of the training process.
        /// </summary>
        bool EnableUserToItemRecommendations { get; }

        /// <summary>
        /// Allow seed items (input items to the recommendation request) to be returned as part of the recommendation results.
        /// </summary>
        bool AllowSeedItemsInRecommendations { get; }

        /// <summary>
        /// The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events.
        /// </summary>
        int DecayPeriodInDays { get; }
    }
}