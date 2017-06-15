// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { Button, ControlLabel, Form, FormControl, FormGroup, Modal, Table } from 'react-bootstrap';

let toValidationState = b => b? 'success': 'error';
let isNullableInt = v => /^\d*$/.test(v);
let isNotEmpty = v => /^.+$/.test(v);
let validateIsNullableInt = v => toValidationState(isNullableInt(v));
let validateIsNotEmpty = v => toValidationState(isNotEmpty(v));
let nullableString = str => (!str? undefined: str);
let nullableBool = str => (!str? undefined: ('true' === str.toLowerCase()));
let nullableInt = str => (!str? undefined: parseInt(str, 10));
let convertModelArgs = strArgs => {
    return {
      description:                              nullableString(strArgs.description),
      blobContainerName:                                       strArgs.blobContainerName,
      catalogFileRelativePath:              nullableString(strArgs.catalogFileRelativePath),
      usageRelativePath:                             strArgs.usageRelativePath,
      evaluationUsageRelativePath:    nullableString(strArgs.evaluationUsageRelativePath),
      supportThreshold:                         nullableInt   (strArgs.supportThreshold),
      cooccurrenceUnit:                         nullableString(strArgs.cooccurrenceUnit),
      similarityFunction:                       nullableString(strArgs.similarityFunction),
      enableColdItemPlacement:                  nullableBool  (strArgs.enableColdItemPlacement),
      enableColdToColdRecommendations:          nullableBool  (strArgs.enableColdToColdRecommendations),
      enableUserAffinity:                       nullableBool  (strArgs.enableUserAffinity),
      enableUserToItemRecommendations:          nullableBool  (strArgs.enableUserToItemRecommendations),
      allowSeedItemsInRecommendations:          nullableBool  (strArgs.allowSeedItemsInRecommendations),
      enableBackfilling:                        nullableBool  (strArgs.enableBackfilling),
      decayPeriodInDays:                        nullableInt   (strArgs.decayPeriodInDays)
    }
};

class TrainNewModel extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      args: {
        description: '',
        blobContainerName: '',
        catalogFileRelativePath: '',
        usageRelativePath: '',
        evaluationUsageRelativePath: '',
        supportThreshold: '6',
        cooccurrenceUnit: 'User',
        similarityFunction: 'Jaccard',
        enableColdItemPlacement: 'false',
        enableColdToColdRecommendations: 'false',
        enableUserAffinity: 'true',
        enableUserToItemRecommendations: 'false',
        allowSeedItemsInRecommendations: 'false',
        enableBackfilling: 'true',
        decayPeriodInDays: '30'
      }
    };
  }
  
  render() {
    let changeHandler = e => {
      let newArgs = {};
      for (let key in this.state.args)
        newArgs[key] = this.state.args[key];
      newArgs[e.target.id] = e.target.value;
      this.setState({ args: newArgs });
    };
    let argsAreValid = () => {
      return (isNotEmpty(this.state.args.blobContainerName)
        && isNotEmpty(this.state.args.usageRelativePath)
        && isNullableInt(this.state.args.supportThreshold)
        && isNullableInt(this.state.args.decayPeriodInDays));
    };
    let trainNewModel = () => {
      let args = convertModelArgs(this.state.args);
      let okJsonHandler = json => {
        this.props.onHide();
        this.props.refresh();
      };
      let notOkResponseHandler = response => {
        switch (response.status) {
          case 400:
            response.json().then(json => {
              let errors = [];
              for (let param in json.ModelState) {
                json.ModelState[param].forEach(msg => errors.push(msg));
              }
              let msg = `Invalid training arguments:\n ${errors.join("\n\n")}`;
              alert(msg);
            });
            break;
          
          default:
            alert(`Unexpected error ${response.status}: ${response.statusText}`);
            break;
        }
      };
      this.props.trainNewModel(args, okJsonHandler, notOkResponseHandler);
    };
    
    return (
      <Modal bsSize='large' show={this.props.show} onHide={this.props.onHide}>
        <Modal.Header closeButton>
          <Modal.Title>Create New Model</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form>
            <Table className='new-model-params'>
              <tbody>
                <tr>
                  <td className='align-bottom'><ControlLabel>Description</ControlLabel></td>
                  <td className='align-bottom'><ControlLabel>Co-occurrence Unit</ControlLabel></td>
                </tr>
                <tr>
                  <td rowSpan='3'>
                    <FormGroup controlId='description'>
                      <FormControl value={this.state.args.description} componentClass='textarea' placeholder='Model Description (Optional)' onChange={changeHandler} tabIndex='1' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='cooccurrenceUnit'>
                      <FormControl value={this.state.args.cooccurrenceUnit} componentClass='select' onChange={changeHandler} tabIndex='8'>
                        <option value='User'>User</option>
                        <option value='Timestamp'>Timestamp</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'><ControlLabel>Similarity Function</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='similarityFunction'>
                      <FormControl value={this.state.args.similarityFunction} componentClass='select' onChange={changeHandler} tabIndex='9'>
                        <option value='Jaccard'>Jaccard</option>
                        <option value='Cooccurrence'>Cooccurrence</option>
                        <option value='Lift'>Lift</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'>
                    <ControlLabel>Blob Container Name <span className='normal'>(required)</span></ControlLabel>
                    <br />
                    <span className='normal'>The name of the container where you stored the training data.</span>
                  </td>
                  <td className='align-bottom'><ControlLabel>Enable Cold Item Placement</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='blobContainerName' validationState={validateIsNotEmpty(this.state.args.blobContainerName)}>
                      <FormControl value={this.state.args.blobContainerName} componentClass='input' placeholder='Training Data Blob Container' onChange={changeHandler} tabIndex='2' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='enableColdItemPlacement'>
                      <FormControl value={this.state.args.enableColdItemPlacement} componentClass='select' onChange={changeHandler} tabIndex='10'>
                        <option value='true'>True</option>
                        <option value='false'>False</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'><ControlLabel>Usage Folder\File Relative Path<span className='normal'>(required)</span></ControlLabel></td>
                  <td className='align-bottom'><ControlLabel>Enable Cold to Cold Recommendations</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='usageRelativePath' validationState={validateIsNotEmpty(this.state.args.usageRelativePath)}>
                      <FormControl value={this.state.args.usageRelativePath} componentClass='input' placeholder='Usage Folder\File Path in Blob Container' onChange={changeHandler} tabIndex='3' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='enableColdToColdRecommendations'>
                      <FormControl value={this.state.args.enableColdToColdRecommendations} componentClass='select' placeholder={true} onChange={changeHandler} disabled={'true' !== this.state.args.enableColdItemPlacement} tabIndex='11'>
                        <option value='true'>True</option>
                        <option value='false'>False</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'><ControlLabel>Evaluation Usage Folder\File Relative Path</ControlLabel></td>
                  <td className='align-bottom'><ControlLabel>Enable User Affinity</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='evaluationUsageRelativePath'>
                      <FormControl value={this.state.args.evaluationUsageRelativePath} componentClass='input' placeholder='Evaluation Usage Folder\File Path in Blob Container (Optional)' onChange={changeHandler} tabIndex='4' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='enableUserAffinity'>
                      <FormControl value={this.state.args.enableUserAffinity} componentClass='select' placeholder={true} onChange={changeHandler} tabIndex='12'>
                        <option value='true'>True</option>
                        <option value='false'>False</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'><ControlLabel>Catalog File Relative Path</ControlLabel></td>
                  <td className='align-bottom'><ControlLabel>Enable User to Item Recommendations</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='catalogFileRelativePath'>
                      <FormControl value={this.state.args.catalogFileRelativePath} componentClass='input' placeholder='Catalog File Path in Blob Container (Optional)' onChange={changeHandler} tabIndex='5' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='enableUserToItemRecommendations'>
                      <FormControl value={this.state.args.enableUserToItemRecommendations} componentClass='select' placeholder={true} onChange={changeHandler} tabIndex='13'>
                        <option value='true'>True</option>
                        <option value='false'>False</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'><ControlLabel>Support Threshold</ControlLabel></td>
                  <td className='align-bottom'><ControlLabel>Allow Seed Items in Recommendations</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='supportThreshold' validationState={validateIsNullableInt(this.state.args.supportThreshold)}>
                      <FormControl value={this.state.args.supportThreshold} componentClass='input' placeholder='Support Threshold (Optional)' onChange={changeHandler} tabIndex='6' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='allowSeedItemsInRecommendations'>
                      <FormControl value={this.state.args.allowSeedItemsInRecommendations} componentClass='select' placeholder={true} onChange={changeHandler} tabIndex='14'>
                        <option value='true'>True</option>
                        <option value='false'>False</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td className='align-bottom'><ControlLabel>Decay Period in Days</ControlLabel></td>
                  <td className='align-bottom'><ControlLabel>Enable Backfilling</ControlLabel></td>
                </tr>
                <tr>
                  <td>
                    <FormGroup controlId='decayPeriodInDays' validationState={validateIsNullableInt(this.state.args.decayPeriodInDays)}>
                      <FormControl value={this.state.args.decayPeriodInDays} componentClass='input' placeholder='Decay Period in Days (Optional)' onChange={changeHandler} tabIndex='7' />
                    </FormGroup>
                  </td>
                  <td>
                    <FormGroup controlId='enableBackfilling'>
                      <FormControl value={this.state.args.enableBackfilling} componentClass='select' placeholder={true} onChange={changeHandler} tabIndex='15'>
                        <option value='true'>True</option>
                        <option value='false'>False</option>
                      </FormControl>
                    </FormGroup>
                  </td>
                </tr>
                
                <tr>
                  <td><a href='https://go.microsoft.com/fwlink/?linkid=849030#model-training-parameters-schema' target='_blank' tabIndex='16'>Model Parameters Documentation</a></td>
                  <td className='align-right'><Button onClick={trainNewModel} bsStyle='primary' disabled={!argsAreValid()} tabIndex='16'>TRAIN</Button></td>
                </tr>
              </tbody>
            </Table>
          </Form>
        </Modal.Body>
      </Modal>
    );
  }
}

export default TrainNewModel;