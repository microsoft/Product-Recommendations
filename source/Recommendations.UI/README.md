# Recommendations Service UI

This folder contains the source code of the recommendation service UI. You can use these to update your recommendations website.

## Prerequisites
You must install [Node.JS](https://nodejs.org/) in order to build the website.

## Building
After making changes, you can use the **BuildAndCopyOutput.cmd** script to rebuild and copy the output to the service code.

The script will basically do the following:
1. Download the required node packages using the command ````npm install package````
2. Build the Recommendations UI using the command ````npm run build````
3. Replace the files under *..\Recommendations.WebApp\UI\* with the newly built files

> **Deployment** Once the UI is built, the Recommendations service code (specifically the *Recommendations.WebApp* project) must be rebuilt and published for the changes to take effect
