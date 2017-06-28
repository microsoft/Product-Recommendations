// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { Button, Table } from 'react-bootstrap';
import { Route, Switch } from 'react-router-dom';
import { Bar } from 'react-chartjs-2';
import ScoreItems from './ScoreItems.js';
import ScoreInput from './ScoreInput.js';
import ScoreResults from './ScoreResults.js';

function display(obj) {
  let type = typeof(obj);
  if (type === 'number')
    return obj.toString();
  if (type === 'boolean')
    return obj? 'True': 'False';
  if (type === 'undefined')
    return '';
  if (type === 'object') {
    if (obj.constructor === Array) {
      return obj.join(', ');
    }
    return '';
  }
  if (type === 'string') {
    let matchTimeDuration = obj.match(/^(..):(..):(..)\.(..).*$/);
    if (matchTimeDuration != null) {
      return `${matchTimeDuration[1]}:${matchTimeDuration[2]}:${matchTimeDuration[3]}.${matchTimeDuration[4]}`;
    }
  }
  return obj;
}

function toChartBarArgs(src) {
  let labels = [];
  let data = [];
  for (let i = 0; i < src.length; i++) {
    labels.push(`${src[i].min}-${src[i].max}`);
    data.push(src[i].percentage);
  }
  return {
    data: {
      labels: labels,
      datasets: [
        {
          data: data,
          backgroundColor: 'rgba(0, 176, 160, 1)'
        }
      ]
    },
    options: {
      scales: {
        xAxes: [
          {
            scaleLabel: {
              display: true,
              labelString: 'Percentile Bucket'
            },
            barPercentage: 0.3
          }
        ],
        yAxes:[
          {
            scaleLabel: {
              display: true,
              labelString: 'Recommendations Served'
            }
          }
        ]
      },
      legend: {
        display: false
      }
    }
  };
}

function toPrecisionTable(src) {
  let k = [ <th key='k'>K</th> ];
  let percentage = [ <td key='percentage'>Percentage</td> ];
  let usersInTest = [ <td key='users'>Users in Test</td> ];
  for (let i = 0; i < src.length; i++) {
    k.push(<th key={i}>{src[i].k}</th>);
    percentage.push(<td key={i}>{src[i].percentage}</td>);
    usersInTest.push(<td key={i}>{src[i].usersInTest}</td>);
  }
  return {
    k: k,
    percentage: percentage,
    usersInTest: usersInTest
  };
}

function objToPropertiesArray(obj) {
    var properties = [];
    let type = typeof(obj);
    if (type === 'object') {
        if (obj.constructor === Array) {
            var len = obj.length;
            for (var i = 0; i < len; i++) {
                properties.push({ name: '', value: obj[i]});
            }
        } else {
            for (var key in obj) {
                properties.push({ name: key, value: obj[key]});
            }        
        }
    }
    
    return properties;
}

class Model extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      i2i: '',
      i2iNumberOfResults: '10',
      i2iResults: null,
      u2i: '',
      u2iUserId: '',
      u2iNumberOfResults: '10',
      u2iResults: null
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
    let updateI2I = e => mergeState({ i2i: e.target.value });
    let updateI2INumberOfResults = e => mergeState({ i2iNumberOfResults: e.target.value });
    let updateU2IUserId = e => mergeState({ u2iUserId: e.target.value });
    let updateU2I = e => mergeState({ u2i: e.target.value });
    let updateU2INumberOfResults = e => mergeState({ u2iNumberOfResults: e.target.value });
    let getI2I = () => {
      let items = this.state.i2i.split(',').filter(s => !!s).map(item => { return { itemId: item.trim() }; });
      for (let i in items) {
        if (!items[i].itemId.match(/^[-_0-9A-Za-z]{1,450}$/)) {
          alert('Item ID lengths can be at most 450, and can only contain alphanumeric characters, dashes and underscores.');
          return;
        }
      }
      this.props.model.getRecommendations(
        items,
        '',
        this.state.i2iNumberOfResults,
        json => { mergeState({ i2iResults: json }) },
        response => {
            response.json().then(json => alert(`${response.statusText}: ${json.Message}`));
            mergeState({ i2iResults: null });
        }
      );
    };
    let getU2I = () => {
      try {
        let items = [];
        if (this.state.u2i.trim() !== '') {
          items = JSON.parse(this.state.u2i);
          for (let i in items) {
            if (!items[i].itemId.match(/^[-_0-9A-Za-z]{1,450}$/)) {
              alert('Item ID lengths must be between 1 and 450, and can only contain alphanumeric characters, dashes and underscores.');
              return;
            }
          }
        }
        this.props.model.getRecommendations(
          items,
          this.state.u2iUserId,
          this.state.u2iNumberOfResults,
          json => { mergeState({ u2iResults: json }) },
          response => {
            response.json().then(json => alert(`${response.statusText}: ${json.Message}`));
            mergeState({ u2iResults: null });
          }
        );
      }
      catch(e) {
        alert('Invalid JSON format!');
      }
    };
    return (
      <div className='model-container'>
        <div className='models-title'>
          <h3>Model {this.props.model.id}</h3>
          <p>{this.props.model.description? this.props.model.description: null}</p>
        </div>
        
        <Route path='/:modelId/:tab' children={props => (
          <div className='model-nav'>
            <ul>
              <li>
                <a
                  className={props.match.params.tab === 'info'? 'selected unclickable': null}
                  onClick={this.props.model.info}>
                  MODEL INFORMATION
                </a>
              </li>
              <li>
                <a
                  className={props.match.params.tab === 'score'? 'selected unclickable': this.props.model.modelStatus !== 'Completed'? 'unclickable': null}
                  onClick={() => (this.props.model.modelStatus === 'Completed') && this.props.model.score()}>
                  SCORE
                </a>
              </li>
              <li>
                <a
                  className={props.match.params.tab === 'eval'? 'selected unclickable': this.props.model.modelStatus !== 'Completed'? 'unclickable': null}
                  onClick={() => (this.props.model.modelStatus === 'Completed') && this.props.model.eval()}>
                  EVALUATION
                </a>
              </li>
            </ul>
          </div>
        )} />
        
        <Switch>
          <Route path='/:modelId/info' render={props => (
            <div>
              <h5>Model Information</h5>
              <Table className='data model-params'>
                <tbody>
                  <tr><td></td></tr>
                  <tr><td><b>Current Status</b></td><td><b>Status Message</b></td></tr>
                  <tr><td>{display(this.props.model.modelStatus)}</td><td>{display(this.props.model.modelStatusMessage)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Creation Date (UTC)</b></td><td><b></b></td></tr>
                  <tr><td>{display(new Date(this.props.model.creationTime).toLocaleString('en-US', { timeZone: 'UTC', year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit', second: '2-digit', hour12: true }))}</td><td></td></tr>
                  <tr><td></td></tr>
                </tbody>
              </Table>
              
              {
                !!this.props.model.statistics?
                  <h5>Data Statistics</h5>:
                  null
              }
              {
                !!this.props.model.statistics?
                  <Table className='data model-params'>
                    <tbody>
                      <tr><td></td></tr>
                      <tr><td><b>Total Duration</b></td><td><b>Training Duration</b></td></tr>
                      <tr><td>{display(this.props.model.statistics.totalDuration)}</td><td>{display(this.props.model.statistics.trainingDuration)}</td></tr>
                      <tr><td></td></tr>
                      <tr><td><b>Number of Users</b></td><td><b>Number of Items</b></td></tr>
                      <tr><td>{display(this.props.model.statistics.numberOfUsers)}</td><td>{display(this.props.model.statistics.numberOfUsageItems)}</td></tr>
                      <tr><td></td></tr>
                      {
                        (() => {
                          let rows = [];
                          if (!!this.props.model.statistics.usageEventsParsing) {
                            rows.push(<tr key='usageEventsParsingLinesParsedTitle'><td><b>Usage Lines Parsed</b></td><td><b>Usage Parsing Duration</b></td></tr>);
                            rows.push(<tr key='usageEventsParsingLinesParsedValue'><td>{display(this.props.model.statistics.usageEventsParsing.totalLinesCount)}</td><td>{display(this.props.model.statistics.usageEventsParsing.duration)}</td></tr>);
                            rows.push(<tr key='usageEventsParsingLinesParsedSpacer'><td></td></tr>);
                            rows.push(<tr key='usageEventsParsingSuccessfulLinesTitle'><td><b>Successful Usage Lines</b></td><td><b>Number of Catalog Items</b></td></tr>);
                            rows.push(<tr key='usageEventsParsingSuccessfulLinesValue'><td>{display(this.props.model.statistics.usageEventsParsing.successfulLinesCount)}</td><td>{display(this.props.model.statistics.numberOfCatalogItems)}</td></tr>);
                            rows.push(<tr key='usageEventsParsingSuccessfulLinesSpacer'><td></td></tr>);
                            
                            if (!!this.props.model.statistics.usageEventsParsing.errors && this.props.model.statistics.usageEventsParsing.errors.length > 0) {
                              rows.push(<tr key='usageEventsParsingErrorsTitle'><td colSpan={2}><b>Usage Parsing Errors</b></td></tr>);
                              rows.push(<tr key='usageEventsParsingErrorsValue'>
                                <td colSpan={2}>
                                  {this.props.model.statistics.usageEventsParsing.errors.map(error => (
                                    <div key={error.error} className='parsing-error'>
                                      <b>{error.error}</b> ({error.count})
                                      <br />
                                      <b>Sample:</b> {error.sample.file}:{error.sample.line}
                                    </div>
                                  ))}
                                </td></tr>);
                              rows.push(<tr key='usageEventsParsingErrorsSpacer'><td></td></tr>);
                            }
                          }
                          return rows;
                        })()
                      }
                      
                      {
                        (() => {
                          let rows = [];
                          if (!!this.props.model.statistics.catalogParsing) {
                            if (!!this.props.model.statistics.numberOfCatalogItems) {
                              rows.push(<tr key='catalogItemsTitle'><td><b>Catalog Lines Parsed</b></td><td><b>Catalog Coverage</b></td></tr>);
                              rows.push(<tr key='catalogItemsValue'><td>{display(this.props.model.statistics.catalogParsing.totalLinesCount)}</td><td>{display((this.props.model.statistics.catalogCoverage*100)+'%')}</td></tr>);
                              rows.push(<tr key='catalogItemsSpacer'><td></td></tr>);
                            }

                            rows.push(<tr key='catalogParsingReportTitle1'><td><b>Successful Catalog Lines</b></td><td><b>Catalog Parsing Duration</b></td></tr>);
                            rows.push(<tr key='catalogParsingReportValue1'><td>{display(this.props.model.statistics.catalogParsing.successfulLinesCount)}</td><td>{display(this.props.model.statistics.catalogParsing.duration)}</td></tr>);
                            rows.push(<tr key='catalogParsingReportSpacer1'><td></td></tr>);
                            
                            if (!!this.props.model.statistics.catalogFeatureWeights) {
                              rows.push(<tr key='catalogFeatureWeightsTitle'><td colSpan={2}><b>Catalog Feature Weights</b></td></tr>);
                              rows.push(<tr key='catalogFeatureWeightsValue'>
                                <td colSpan={2}>
                                  {objToPropertiesArray(this.props.model.statistics.catalogFeatureWeights)
                                  .sort(function(f1, f2){return Math.abs(f2.value) - Math.abs(f1.value)})
                                  .map(feature => (
                                    <div key={feature.name} className='feature-weight'>
                                      <b>{feature.name}</b>: {feature.value}
                                    </div>
                                  ))}
                                </td></tr>);
                              rows.push(<tr key='catalogFeatureWeightsSpacer'><td></td></tr>);
                            }

                            if (!!this.props.model.statistics.catalogParsing.errors && this.props.model.statistics.catalogParsing.errors.length > 0) {
                              rows.push(<tr key='catalogParsingErrorsTitle'><td colSpan={2}><b>Catalog Parsing Errors</b></td></tr>);
                              rows.push(<tr key='catalogParsingErrorsValue'>
                                <td colSpan={2}>
                                  {this.props.model.statistics.catalogParsing.errors.map(error => (
                                    <div key={error.error} className='parsing-error'>
                                      <b>{error.error}</b> ({error.count})
                                      <br />
                                      <b>Sample:</b> {error.sample.file}:{error.sample.line}
                                    </div>
                                  ))}
                                </td></tr>);
                              rows.push(<tr key='catalogParsingErrorsSpacer'><td></td></tr>);
                            }
                          }
                          return rows;
                        })()
                      }
                    </tbody>
                  </Table>:
                  null
              }
              
              <h5>Model Parameters</h5>
              <Table className='data model-params'>
                <tbody>
                  <tr><td></td></tr>
                  <tr><td><b>Description</b></td><td><b>Co-occurrence Unit</b></td></tr>
                  <tr><td>{display(this.props.model.description)}</td><td>{display(this.props.model.parameters.cooccurrenceUnit)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Blob Container Name</b></td><td><b>Similarity Function</b></td></tr>
                  <tr><td>{display(this.props.model.parameters.blobContainerName)}</td><td>{display(this.props.model.parameters.similarityFunction)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Usage Folder\File Relative Path</b></td><td><b>Enable Cold Item Placement</b></td></tr>
                  <tr><td>{display(this.props.model.parameters.usageRelativePath)}</td><td>{display(this.props.model.parameters.enableColdItemPlacement)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Catalog File Relative Path</b></td><td><b>Enable Cold to Cold Recommendations</b></td></tr>
                  <tr><td>{display(this.props.model.parameters.catalogFileRelativePath)}</td><td>{display(this.props.model.parameters.enableColdToColdRecommendations)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Evaluation Usage Folder\File Relative Path</b></td><td><b>Enable User Affinity</b></td></tr>
                  <tr><td>{display(this.props.model.parameters.evaluationUsageRelativePath)}</td><td>{display(this.props.model.parameters.enableUserAffinity)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Support Threshold</b></td><td><b>Allow Seed Items in Recommendations</b></td></tr>
                  <tr><td>{display(this.props.model.parameters.supportThreshold)}</td><td>{display(this.props.model.parameters.allowSeedItemsInRecommendations)}</td></tr>
                  <tr><td></td></tr>
                  <tr><td><b>Decay Period in Days</b></td><td><b>Enable Backfilling</b></td></tr>
                  <tr><td>{display(this.props.model.parameters.decayPeriodInDays)}</td><td>{display(this.props.model.parameters.enableBackfilling)}</td></tr>
                  <tr><td></td></tr>
                </tbody>
              </Table>
            </div>
          )} />
          
          <Route path='/:modelId/score' render={props => (
            <div>
              <h5>Test Item Recommendations</h5>
              <Table className='data'>
                <tbody>
                  <tr>
                    <td>
                      <ScoreItems
                        title={'Item Id(s)'}
                        text={'Enter list of itemIds'}
                        example={'DHQ-345, RS-987, NPO-287'}
                        value={this.state.i2i}
                        onChange={updateI2I}
                      />
                    </td>
                    <td rowSpan={2} className='i2i'><ScoreResults results={this.state.i2iResults} /></td>
                  </tr>
                  <tr>
                    <td><ScoreInput title={'Number of Recommendations'} value={this.state.i2iNumberOfResults} onChange={updateI2INumberOfResults} /></td>
                  </tr>
                  <tr>
                    <td>
                      <Table className='data'><tbody><tr><td><Button bsStyle='primary' onClick={getI2I} disabled={this.props.model.modelStatus !== 'Completed'}>GET RECOMMENDATIONS</Button></td></tr></tbody></Table>
                    </td>
                  </tr>
                </tbody>
              </Table>
              
              <h5>Test Personalized Recommendations</h5>
              <Table className='data'>
                <tbody>
                  <tr>
                    <td><ScoreInput title={'User Id (Optional)'} value={this.state.u2iUserId} onChange={updateU2IUserId} /></td>
                    <td rowSpan={3} className='u2i'><ScoreResults results={this.state.u2iResults} /></td>
                  </tr>
                  <tr>
                    <td>
                      <ScoreItems
                        title={'User Transaction(s)'}
                        text={'Enter transactions in JSON format as expected by the API'}
                        example={'[{"itemId": "ItemId123", "eventType": "Click", "timestamp": "2017-01-31T23:59:59"}]'}
                        value={this.state.u2i}
                        onChange={updateU2I} />
                    </td>
                  </tr>
                  <tr>
                    <td><ScoreInput title={'Number of Recommendations'} value={this.state.u2iNumberOfResults} onChange={updateU2INumberOfResults} /></td>
                  </tr>
                  <tr>
                    <td>
                      <Table className='data'><tbody><tr><td><Button bsStyle='primary' onClick={getU2I} disabled={this.props.model.modelStatus !== 'Completed'}>GET RECOMMENDATIONS</Button></td></tr></tbody></Table>
                    </td>
                  </tr>
                </tbody>
              </Table>
            </div>
          )} />
          
          <Route path='/:modelId/eval' render={props => {
            if (!this.props.model.statistics.evaluation) {
              return (
                <div className='metrics'>
                  <br />
                  <p>
                    You need to provide an <a href='https://go.microsoft.com/fwlink/?linkid=849030#model-evaluation-schema'>evaluation file</a> when you train the model in order to see evaluation results.
                  </p>
                </div>
              );
            }
            
            let barArgs = toChartBarArgs(this.props.model.statistics.evaluation.metrics.diversityMetrics.percentileBuckets);
            let precisionTable = toPrecisionTable(this.props.model.statistics.evaluation.metrics.precisionMetrics);
            return (
              <div className='metrics'>
                <br />
                <Table className='data desc-data'>
                  <tbody>
                    <tr>
                      <td>
                        <h3>Diversity (User Recommendations)</h3>
                        <p>Diversity measures the distribution of items recommended. Each percentile bucket is represented by a span (min/max values that range between 0 and 100).
                        The items close to 0 are the least popular. For instance, if the percentage value for the 99-100 percentile bucket is 10.6, it means that 10.6 percent of
                        the recommendations returned only the top 1% most popular items. The percentile bucket min value is inclusive, and the max value is exclusive except for 100.</p>
                      </td>
                      <td>
                        <Bar data={barArgs.data} options={barArgs.options} />
                        <br />
                        <Table className='metrics-data'>
                          <thead><tr><th></th><th></th></tr></thead>
                          <tbody>
                            <tr><td>Total Items Recommended</td><td>{this.props.model.statistics.evaluation.metrics.diversityMetrics.totalItemsRecommended}</td></tr>
                            <tr><td>Unique Items in Train Set</td><td>{this.props.model.statistics.evaluation.metrics.diversityMetrics.uniqueItemsInTrainSet}</td></tr>
                            <tr><td>Unique Items Recommended</td><td>{this.props.model.statistics.evaluation.metrics.diversityMetrics.uniqueItemsRecommended}</td></tr>
                          </tbody>
                        </Table>
                      </td>
                    </tr>
                  </tbody>
                </Table>
                <br />
                <Table className='data desc-data'>
                  <tbody>
                    <tr>
                      <td>
                        <h3>Precision at K (User Recommendations)</h3>
                        <p>K represents the number of recommendations shown to the customer. So, if the Percentage under 5 is 4.94, the table would reads as follows:
                        "if during the test period, only 5 item-based recommendations would have been shown to the customers, 4.94 of the users would have actually
                        purchased at least one recommended item".</p>
                      </td>
                      <td>
                        <Table className='metrics-data'>
                          <thead><tr>{precisionTable.k}</tr></thead>
                          <tbody>
                            <tr>{precisionTable.percentage}</tr>
                            <tr>{precisionTable.usersInTest}</tr>
                          </tbody>
                        </Table>
                      </td>
                    </tr>
                  </tbody>
                </Table>

                <h5>Evaluation Usage Parsing Report</h5>
                <Table className='data model-params'>
                  <tbody>
                    <tr><td></td></tr>
                    <tr><td><b>Evaluation Usage Lines Parsed</b></td><td><b>Evaluation Usage Parsing Duration</b></td></tr>
                    <tr><td>{display(this.props.model.statistics.evaluation.usageEventsParsing.totalLinesCount)}</td><td>{display(this.props.model.statistics.evaluation.usageEventsParsing.duration)}</td></tr>
                    <tr><td></td></tr>
                    <tr><td><b>Successful Evaluation Usage Lines</b></td><td><b></b></td></tr>
                    <tr><td>{display(this.props.model.statistics.evaluation.usageEventsParsing.successfulLinesCount)}</td><td></td></tr>
                    <tr><td></td></tr>
                    {
                      (() => {
                        let rows = [];
                        if (!!this.props.model.statistics.evaluation.usageEventsParsing.errors && this.props.model.statistics.evaluation.usageEventsParsing.errors.length > 0) {
                          rows.push(<tr key='evaluationUsageParsingErrorsTitle'><td colSpan={2}><b>Evaluation Usage Parsing Errors</b></td></tr>);
                          rows.push(<tr key='evaluationUsageParsingErrorsValue'>
                            <td colSpan={2}>
                            {
                              this.props.model.statistics.evaluation.usageEventsParsing.errors.map(error => (
                                <div key={error.error} className='parsing-error'>
                                  <b>{error.error}</b> ({error.count})
                                  <br />
                                 <b>Sample:</b> {error.sample.file}:{error.sample.line}
                                </div>
                              ))
                            }
                            </td>
                          </tr>);
                          rows.push(<tr key='evaluationUsageParsingErrorsSpacer'><td></td></tr>);
                        }
                        return rows;
                      })()
                    }
                  </tbody>
                </Table>
              </div>
            );
          }} />
        </Switch>
      </div>
    );
  }
}

export default Model;