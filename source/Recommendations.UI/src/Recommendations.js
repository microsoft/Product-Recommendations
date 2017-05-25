// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

function toQuery(query) {
  if (!query) return '';
  let qs = [];
  for (let q in query) {
    qs.push(`${q}=${encodeURIComponent(query[q])}`);
  }
  return qs.join('&');
}

class Recommendations {
  constructor(serviceUri, adminKey) {
    this.serviceUri = serviceUri;
    this.adminKey = adminKey;
    this.headers = new Headers({ 'Content-Type': 'application/json', 'X-Api-Key': adminKey });
    this.send = (method, url, query, body) => {
      return fetch(`${url}?${toQuery(query)}`, { method: method, headers: this.headers, body: body == null? undefined: JSON.stringify(body) });
    }
    this.get = (url, query) => this.send("GET", url, query, null);
    this.post = (url, query, body) => this.send("POST", url, query, body);
    this.put = (url, query, body) => this.send("PUT", url, query, body);
  }
  
  processJsonResponse(request, okJsonHandler, notOkRequestHandler) {
    return new Promise((resolve, reject) => {
      request.then(response => {
        if (response.ok) {
          response.json().then(json => resolve(okJsonHandler(json)));
        }
        else {
          resolve(notOkRequestHandler(response));
        }
      })
    });
  }
  
  getModels() {
    return this.get(`${this.serviceUri}/api/models`, null);
  }
  
  getModel(modelId) {
    return this.get(`${this.serviceUri}/api/models/${modelId}`, null);
  }
  
  getDefaultModel() {
    return this.get(`${this.serviceUri}/api/models/default`, null);
  }
  
  setDefaultModel(modelId) {
    return this.put(`${this.serviceUri}/api/models/default`, { modelId: modelId }, null);
  }
  
  trainModel(modelParameters) {
    return this.post(`${this.serviceUri}/api/models`, null, modelParameters);
  }
  
  deleteModel(modelId) {
    return this.send("DELETE", `${this.serviceUri}/api/models/${modelId}`, null, null);
  }
  
  getRecommendations(modelId, items, userId, numberOfResults) {
    return this.post(`${this.serviceUri}/api/models/${modelId}/recommend`, { userId: userId, recommendationCount: numberOfResults }, items );
  }
}

export default Recommendations;