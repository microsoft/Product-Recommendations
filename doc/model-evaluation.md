### Offline evaluation

The goal of an offline evaluation is to predict precision (the number of users that will purchase one of the recommended items) and the diversity of recommendations (the number of items that are recommended). These also help you tune the various model training parameters and evaluate which one works best.

These metrics will be automatically computed if **evaluation files are provided** as part of model training input - See parameter *evaluationUsageRelativePath * in [API Reference](api-reference.md#create-a-model). Common splitting strategies can be found at the end of this document.

---
#### Precision-at-k
The following table represents the output of the precision-at-k offline evaluation.

| k | Percentage | Users in test |
| :-: | :-: | :-: |
| 1 | 13.75 | 10,000
| 2 | 18.04 | 10,000
| 3 | 21.00 | 10,000
| 4 | 24.31 | 10,000
| 5 | 26.61 | 10,000

##### k
In the preceding table, k represents the number of recommendations shown to the customer. The table reads as follows: “If during the test period, only one recommendation was shown to the customers, only 13.75 of the users would have purchased that recommendation.” This statement is based on the assumption that the model was trained with purchase data. Another way to say this is that the precision at 1 is 13.75.

You will notice that as more items are shown to the customer, the likelihood of the customer purchasing a recommended item goes up. For the preceding experiment, the probability almost doubles to 26.61 percent when 5 items are recommended.

##### Percentage
The percentage of users that interacted with at least one of the k recommendations is shown. The percentage is calculated by dividing the number of users that interacted with at least one recommendation by the total number of users considered. See Users considered for more information.

##### Users in test
Data in this row represents the total number of users in the test dataset.

---
#### Diversity
Diversity metrics measure the type of items recommended. The following table represents the output of the diversity offline evaluation.

| Percentile bucket | Percentage 
| :-: | :-: 
| 0-90 | 34.258
| 90-99 | 55.127 
| 99-100 | 10.615 

**Total items recommended:** 100,000

**Unique items recommended:** 954

##### Percentile buckets
Each percentile bucket is represented by a span (minimum and maximum values that range between 0 and 100). The items close to 100 are the most popular items, and the items close to 0 are the least popular. For instance, if the percentage value for the 99-100 percentile bucket is 10.6, it means that 10.6 percent of the recommendations returned only the top one percent most popular items. The percentile bucket minimum value is inclusive, and the maximum value is exclusive, except for 100.

##### Unique items recommended
The unique items recommended metric shows the number of distinct items that were returned for evaluation.

##### Total items recommended
The total items recommended metric shows the number of items recommended. Some may be duplicates.

---

### Common splitting strategies 

These are the three common splitting strategies that can be used to split usage data into train and test set. For a good evaluation atleast a 1000 and atmost a 10,000 users are recommended to be present in test data.

1. **Random** – for each *User* with at least 3 unique Items, randomly put 1 of the Items in Test, the remaining in Train. If more than 10,000 such Users, randomly select 10,000.
2. **Last Event** – for each *User* with at least 3 unique Items, take the Item with the latest time stamp and put it in Test (in case of ties, randomly take 1 Item from the ones with the same latest timestamp), the remaining in Train. If more than 10,000 such Users, randomly select 10,000 and put the LastEvent of the remaining back in Train.
3. **Fixed Date** – for each *User*, put all events before the date in Train, and all events after the date in Test.
