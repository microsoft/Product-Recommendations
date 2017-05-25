// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { FormControl, Table } from 'react-bootstrap';

export default function ScoreItems(props) {
  return (
    <Table className='data'>
      <tbody>
        <tr><td><b>{props.title}</b><br />{props.text}, for example: <i>{props.example}</i></td></tr>
        <tr><td className='items'><FormControl componentClass='textarea' value={props.value} onChange={props.onChange} /></td></tr>
      </tbody>
    </Table>
  );
}