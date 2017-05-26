// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import React from 'react';
import { Table } from 'react-bootstrap';

export default function ScoreResults(props) {
  return (
    <Table className='data'>
      <tbody>
        <tr><td><b>Results</b></td></tr>
        <tr><td className='results'>
          <div>
          {
            props.results == null?
              null:
              (
                props.results.length === 0?
                  'None':
                  <Table className='data'>
                    <tbody>
                    {
                      props.results.map(r => <tr key={r.recommendedItemId}><td>{r.recommendedItemId}</td><td>Score: {r.score}</td></tr>)
                    }
                    </tbody>
                  </Table>
              )
          }
          </div>
        </td></tr>
      </tbody>
    </Table>
  );
}