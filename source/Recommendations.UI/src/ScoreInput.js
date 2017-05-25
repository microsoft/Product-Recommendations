// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { FormControl, Table } from 'react-bootstrap';

export default function ScoreInput(props) {
  return (
    <Table className='data'>
      <tbody>
        <tr><td><b>{props.title}</b></td></tr>
        <tr><td className='count'><FormControl componentClass='input' value={props.value} onChange={props.onChange} /></td></tr>
      </tbody>
    </Table>
  );
}