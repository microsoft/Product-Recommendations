# Benchmarks 

## Train duration on different SKUs

Below are the train duration for a few datasets (based on number of items in catalog/usage files) on different App Service [hosting plans](https://azure.microsoft.com/en-us/pricing/details/app-service/). These are meant to give guidance on what plan should be picked up during setup of the solution or selecting the right one as data sets change.

Note - These are just for references, and actual time will vary based on different conditions which encompass things like build parameters, number of features in catalog etc, to the scoring load on the system.

### Standard/Premium S3 Tier - 4 Cores 7 GB RAM

|Catalog Size|Usage Size|Train Duration|
|:-:|:-:|:-:|
|2K|300K|30 seconds|
|2K|3M|4 minutes|
|15K|5M|1 minute|
|20K|700K|2 minutes|
|25K|300K|2 minutes|
|30K|600K|2 minutes|
|30K|9M|10 minutes|
|35K|4M|4 minutes|
|60K|15M|35 minutes|
|100K|1M|30 minutes|
|400K|1M|6 hours|

---

### Standard/Premium S2 Tier - 2 Cores 3.5 GB RAM

|Catalog Size|Usage Size|Train Duration|
|:-:|:-:|:-:|
|2K|300K|30 seconds|
|2K|3M|5 minutes|
|15K|5M|1 minute|
|20K|700K|3 minutes|
|25K|300K|3 minutes|
|30K|600K|3 minutes|
|30K|9M|n/a|
|35K|4M|4 minutes|
|60K|15M|n/a|
|100K|1M|50 minutes|
|400K|1M|n/a|

---

### Standard/Premium S1 Tier - 1 Core 1.75 GB RAM

|Catalog Size|Usage Size|Train Duration|
|:-:|:-:|:-:|
|2K|300K|30 seconds|
|2K|3M|9 minutes|
|15K|5M|1 minute|
|20K|700K|6 minutes|
|25K|300K|6 minutes|
|30K|600K|6 minutes|
|30K|9M|n/a|
|35K|4M|5 minutes|
|60K|15M|n/a|
|100K|1M|2 hours|
|400K|1M|n/a|

K=*1000  
M=*1000,000

In our measurements, the system was unable to complete the training due to memory constraints on the items marked with "n/a".

---

## Scoring latencies on different SKUs

Below are the scoring latencies for a few combination of load on the system on various SKUs.

Note - These are just for references, and actual time may vary based on size of model, network latency etc. We have also seen the latencies being impacted if a model training is in progress.



|SKU|Instances|Requests (per second)|Latency(msec)| 
|:-:|:-:|:-:|:-:|
|S1|1|100|50-100|
|S3|1|100|50-100|
|S3|1|200|150-250|
|S3|3|100|50-100|
|S3|3|200|50-100|
