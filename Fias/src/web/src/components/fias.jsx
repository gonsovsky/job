import React from 'react';

export default class Fias extends React.Component{

    constructor(props){
        super(props);
    }

    render() {
        return <div className="fias">

                <h5>{this.props.fias.fias_code}</h5>

                <h5>{this.props.fias.address}</h5>


            <h5>{this.props.fias.ipaddr}</h5>

                <h5>{this.props.fias.time}</h5>
                <hr/>
            <br/>
        </div>

    }
}