// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// Initialization
// 1. Read API key from cookie (default empty)
// 2. Try to login with the key and suppress login dialog
// 3. If successful then show models, otherwise prompt for API key

// Refresh Models Table
// 1. After the first successful getModels call, another getModels call is scheduled for 3 seconds later
// 2. If the location.hash is still the root then refresh the table, otherwise do nothing
// 3. Schedule another getModels call for 3 seconds later, go to 2
// Refreshing is done independent of App state changes because the page may rerender from other actions.

// Refresh Training Model
// 1. If location.hash does not match the model being displayed, call getModel
// 2. If the current model is not completed or failed, schedule a getModel call for 3 seconds later
// Refreshing only occurs whenever the App state changes because there are no other paths rerender page.

import React from 'react';
import { Navbar, Nav, NavItem, Button } from 'react-bootstrap';
import { HashRouter, Route, Switch } from 'react-router-dom';
import Cookies from 'universal-cookie';

import Recommendations from './Recommendations.js';
import Login from './Login.js';
import Model from './Model.js';
import Models from './Models.js';
import TrainNewModel from './TrainNewModel.js';
import ConfirmModal from './ConfirmModal.js';

import './App.css';
import loadingImg from '../public/loading.gif';

let links = {
  home: () => (window.location.hash = '/'),
  modelInfo: modelId => (window.location.hash = `/${modelId}/info`),
  modelScore: modelId => (window.location.hash = `/${modelId}/score`),
  modelEval: modelId => (window.location.hash = `/${modelId}/eval`)
};

class App extends React.Component {
  constructor(props) {
    super(props);
    this.cookies = new Cookies();
    
    let apiKey = this.cookies.get('apiKey');
    if (!apiKey) {
      apiKey = '';
    }
    
    this.state = {
      init: true,
      apiKey: apiKey,
      recommendations: null,
      models: null,
      selectedModel: null,
      newModelDialog: false,
      defaultModelId: null,
      modelToDelete: null,
      candidateDefaultModel: null,
      isInvalid: false,
      pendingLogin: false,
      modelOrder: {
        key: 'creationTime',
        ascend: true
      }
    };
  }
  
  render() {
    let mergeState = newState => {
      for (let key in this.state) {
        if (newState[key] === undefined) {
          newState[key] = this.state[key];
        }
      }
      this.setState(newState);
    };
    let compareModels = (a, b) => {
      let va = a[this.state.modelOrder.key];
      let vb = b[this.state.modelOrder.key];
      va = !va? '': va;
      vb = !vb? '': vb;
      let asc = va < vb? -1: va > vb? 1: a.id < b.id? -1: 1;
      return this.state.modelOrder.ascend? asc: -asc;
    };
    let getModels = (recommendations, modelsHandler, errorHandler) => {
      Promise.all([ recommendations.getModels(), recommendations.getDefaultModel() ]).then(promises => {
        let modelsResponse = promises[0];
        let defaultResponse = promises[1];
        
        if (!modelsResponse.ok) {
          errorHandler(modelsHandler, defaultResponse);
          return;
        }
        
        Promise.all([ modelsResponse.json(), defaultResponse.ok? defaultResponse.json(): null ]).then(jsons => {
          jsons[0].forEach(model => {
            model.info = () => links.modelInfo(model.id);
            model.score = () => links.modelScore(model.id);
            model.eval = () => links.modelEval(model.id);
            model.isDefault = !jsons[1]? false: (model.id === jsons[1].id);
            model.setAsDefault = () => mergeState({ candidateDefaultModel: model.id });
            model.deleteModel = () => mergeState({ modelToDelete: model.id });
          });
          modelsHandler(jsons[0], jsons[1]);
        });
      });
    };
    let trySetRecommendations = (recommendations) => {
      if (recommendations == null) return;
      let errorHandler = (modelsResponse, defaultResponse) => mergeState({ pendingLogin: false, init: false, isInvalid: !this.state.init });
      let modelsHandlerFactory = (closureReco, checkSource, refresh, refreshTimeoutMs) =>
        (models, defaultModel) => {
          if (!checkSource || closureReco === this.state.recommendations) {
            this.setState({
              recommendations: recommendations,
              models: models,
              defaultModelId: !!defaultModel? defaultModel.id: null,
              pendingLogin: false,
              init: false,
              isInvalid: false
            });
            this.cookies.set('apiKey', recommendations.adminKey);
            
            setTimeout(() => refresh(refreshTimeoutMs), refreshTimeoutMs);
          }
        };
      let refresh = refreshTimeoutMs => {
        if (window.location.hash === '#/') {
          getModels(recommendations, modelsHandlerFactory(recommendations, true, refresh, refreshTimeoutMs), errorHandler);
        }
        else {
            setTimeout(() => refresh(refreshTimeoutMs), refreshTimeoutMs);
        }
      };
      getModels(recommendations, modelsHandlerFactory(recommendations, false, refresh, 3000), errorHandler);
    };
    let refreshSelectedModel = (modelId, discardResult) => {
      this.state.recommendations.processJsonResponse(
        this.state.recommendations.getModel(modelId),
        json => {
          json.getRecommendations = (items, userId, numberOfResults, okJsonHandler, notOkResponseHandler) =>
            this.state.recommendations.processJsonResponse(
              this.state.recommendations.getRecommendations(modelId, items, userId, numberOfResults),
              okJsonHandler,
              notOkResponseHandler
            );
          json.info = () => links.modelInfo(modelId);
          json.score = () => links.modelScore(modelId);
          json.eval = () => links.modelEval(modelId);
          if (!discardResult()) {
            mergeState({ selectedModel: json });
          }
        },
        response => {
          alert(`Getting model ${modelId} failed with ${response.status}: ${response.statusText}`);
          links.home();
        }
      );
    }
    
    let logout = () => {
      this.cookies.set('apiKey', '');
      mergeState({ apiKey: '', recommendations: null, models: null, selectedModel: null });
    }
    let trySetApiKey = newKey => trySetRecommendations(new Recommendations('', newKey));
    let onChange = newKey => mergeState({ apiKey: newKey, recommendations: null, models: null, selectedModel: null, isInvalid: false });
    let login = () => {
      mergeState({ pendingLogin: true });
      trySetApiKey(this.state.apiKey);
    };
    let showTrainNewModelDialog = () => mergeState({ newModelDialog: true });
    let hideTrainNewModelDialog = () => mergeState({ newModelDialog: false });
    let trainNewModel = (args, okJsonHandler, notOkResponseHandler) => this.state.recommendations.processJsonResponse(this.state.recommendations.trainModel(args), okJsonHandler, notOkResponseHandler);
    let setOrder = newOrder => mergeState({ modelOrder: newOrder });
    
    return (
      this.state.init?
        (() => {
          trySetApiKey(this.state.apiKey);
          return null;
        })():
        <HashRouter>
          <div className='App'>
            <h3 className='App-header'>Recommendations Preconfigured Solution</h3>
            <Navbar>
              <Navbar.Collapse>
                <Nav>
                  <NavItem onClick={links.home}>Models</NavItem>
                  <NavItem id='documentation-link' href='https://go.microsoft.com/fwlink/?linkid=847717' target='_blank'>Documentation</NavItem>
                  <NavItem id='sample-link' href='https://go.microsoft.com/fwlink/?linkid=847717&pc=c-sharp-sample' target='_blank'>Sample Code</NavItem>
                  <NavItem id='reference-link' href='https://go.microsoft.com/fwlink/?linkid=849030' target='_blank'>API Reference</NavItem>
                </Nav>
                <Nav pullRight>
                  <NavItem onClick={logout}>Logout</NavItem>
                </Nav>
              </Navbar.Collapse>
            </Navbar>
            <Login value={this.state.apiKey} show={!this.state.recommendations} onChange={onChange} isInvalid={this.state.isInvalid} pendingLogin={this.state.pendingLogin} login={login} />
            {
              !!this.state.recommendations?
                (
                  <Switch>
                    <Route path='/:modelId' render={ props => {
                        let currentHash = window.location.hash;
                        if (!this.state.selectedModel || (this.state.selectedModel.id !== props.match.params.modelId)) {
                          refreshSelectedModel(props.match.params.modelId, () => window.location.hash !== currentHash);
                          return <img src={loadingImg} alt='Loading' />;
                        }
                        if (this.state.selectedModel.modelStatus !== 'Completed' && this.state.selectedModel.modelStatus !== 'Failed') {
                          setTimeout(() => refreshSelectedModel(props.match.params.modelId, () => window.location.hash !== currentHash), 3000);
                        }
                        return <Model model={this.state.selectedModel} />;
                      }}
                    />
                    <Route path='/' render={ props => {
                        return (
                          <div>
                            <Models models={this.state.models.slice().sort(compareModels)} order={this.state.modelOrder} setOrder={setOrder} />
                            <div className='train-new-model'>
                              <br />
                              <Button bsStyle='primary' onClick={showTrainNewModelDialog}>TRAIN NEW MODEL</Button>
                            </div>
                          </div>
                        );
                      }}
                    />
                  </Switch>
                ):
                null
            }
            <TrainNewModel
              show={this.state.newModelDialog}
              onHide={hideTrainNewModelDialog}
              trainNewModel={trainNewModel}
              refresh={() => mergeState({})} />
            <ConfirmModal
              show={!!this.state.candidateDefaultModel}
              title='Set Default Model'
              text={`Are you sure you want to set model ${this.state.candidateDefaultModel} as the default model?`}
              oldValue={this.state.defaultModelId}
              newValue={this.state.candidateDefaultModel}
              operation={() => this.state.recommendations.setDefaultModel(this.state.candidateDefaultModel)}
              cancel={() => mergeState({ candidateDefaultModel: null })} />
            <ConfirmModal
              show={!!this.state.modelToDelete}
              title='Delete Model'
              text={`Are you sure you want to delete model ${this.state.modelToDelete}?${this.state.modelToDelete === this.state.defaultModelId? ' Note that you will stop receiving recommendations from the default model since you will be deleting it.': ''}`}
              operation={() => this.state.recommendations.deleteModel(this.state.modelToDelete)}
              cancel={() => mergeState({ modelToDelete: null })} />
          </div>
        </HashRouter>
    );
  }
}

export default App;