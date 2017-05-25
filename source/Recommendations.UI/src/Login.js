// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { Button, FormControl, Modal } from 'react-bootstrap';

import loadingImg from '../public/loading.gif';

export default function Login(props) {
  let keyPressHandler = e => {
    if (e.key === 'Enter') {
      props.login();
    }
  };
  return (
    <Modal show={props.show} className='login'>
      <Modal.Header>
        <Modal.Title>Recommendations Preconfigured Solution</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <h5>Welcome! Enter your Admin API Key &nbsp; { props.pendingLogin? <img src={loadingImg} alt='Loading' />: null }</h5>
        <FormControl type='password' tabIndex='1' placeholder='Enter your API key here' value={props.value} onKeyPress={keyPressHandler} onChange={e => props.onChange(e.target.value)} />
        { props.isInvalid? <p className='error-text'>Invalid key</p>: null }
        <br />
        <h5>Forgot your Admin API Key?</h5>
        <p>Find it in the Application settings section of the App Service in the <a target='_blank' href='https://portal.azure.com/'>Azure Portal</a>.</p>
        <br />
        <div className='align-right'><Button bsStyle='primary' tabIndex='2' onClick={props.login}>LOGIN</Button></div>
      </Modal.Body>
    </Modal>
  );
}