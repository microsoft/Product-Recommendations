// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { Button, Radio, Table } from 'react-bootstrap';

export default function Models(props) {
  let prettifyTime = iso8601 => {
    return new Date(iso8601).toLocaleString('en-US', { timeZone: 'UTC', year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit', second: '2-digit', hour12: true });
  };
  if (!props.models) return null;
  let selectArrow = key => props.order.key === key? props.order.ascend? (<span>&#9650;</span>): (<span>&#9660;</span>): (<span>&#12288;</span>);
  let newSort = key => { return { key: key, ascend: props.order.key === key? !props.order.ascend: true }; };
  return (
    <div className='models-container'>
      <div className='models-title'><h3>Models</h3></div>
      <div>
        <Table striped bordered condensed hover className='models'>
          <thead>
            <tr>
              <th className='sortable' onClick={() => props.setOrder(newSort('id'))}><span className='no-wrap'>Model ID {selectArrow('id')}</span></th>
              <th className='sortable max' onClick={() => props.setOrder(newSort('description'))}><span className='no-wrap'>Description {selectArrow('description')}</span></th>
              <th className='sortable' onClick={() => props.setOrder(newSort('modelStatus'))}><span className='no-wrap'>Status {selectArrow('modelStatus')}</span></th>
              <th className='sortable' onClick={() => props.setOrder(newSort('isDefault'))}><span className='no-wrap'>Default Model {selectArrow('isDefault')}</span></th>
              <th className='sortable' onClick={() => props.setOrder(newSort('creationTime'))}><span className='no-wrap'>Creation Date (UTC) {selectArrow('creationTime')}</span></th>
              <th></th>
              <th>&#12288;</th>
            </tr>
          </thead>
          <tbody>
          {
            props.models.map(model =>
              <tr key={model.id}>
                <td onClick={model.info} className='clickable'><span className='no-wrap'>{model.id}</span></td>
                <td onClick={model.info} className='clickable max'>{model.description}</td>
                <td onClick={model.info} className='clickable'><span className='no-wrap'>{model.modelStatus}</span></td>
                <td><Radio name='defaultModel' checked={model.isDefault} onChange={model.setAsDefault} disabled={model.modelStatus !== 'Completed'} /></td>
                <td onClick={model.info} className='clickable'><span className='no-wrap'>{prettifyTime(model.creationTime)}</span></td>
                <td><Button bsStyle='primary' onClick={model.score} disabled={model.modelStatus !== 'Completed'}>SCORE</Button></td>
                <td className='delete-model'><div><a onClick={model.deleteModel}>&times;</a></div></td>
              </tr>
            )
          }
          </tbody>
        </Table>
      </div>
    </div>
  );
}