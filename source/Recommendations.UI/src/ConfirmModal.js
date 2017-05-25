// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { Button, Modal, Table } from 'react-bootstrap';

class ConfirmModal extends React.Component {
  constructor(props) {
    super(props);
    this.state = { waitingForOperation: false };
  }
  
  render() {
    let doOperation = () => {
      this.setState({ waitingForOperation: true });
      this.props.operation().then(() => { this.setState({ waitingForOperation: false }); this.props.cancel(); });
    };
    let onHide = () => this.state.waitingForOperation || this.props.cancel();
    return (
      <Modal bsSize='large' show={this.props.show} onHide={onHide} className={this.state.waitingForOperation && 'wait'}>
        <Modal.Header closeButton={!this.state.waitingForOperation}>
          <Modal.Title>{this.props.title}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>{this.props.text}</p>
          {
            (!!this.props.oldValue || !!this.props.newValue)?
              (
                <Table className='modal-data'>
                  <tbody>
                    <tr><td><b>Current Value:</b></td><td>{this.props.oldValue}</td></tr>
                    <tr><td><b>New Value:</b></td><td>{this.props.newValue}</td></tr>
                  </tbody>
                </Table>
              ):
              null
          }
          <div className='align-right'>
            <Button bsStyle='primary' onClick={doOperation} disabled={this.state.waitingForOperation}>CONFIRM</Button>
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <Button bsStyle='primary' onClick={onHide} disabled={this.state.waitingForOperation}>CANCEL</Button>
          </div>
        </Modal.Body>
      </Modal>
    );
  }
}

export default ConfirmModal;