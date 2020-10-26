import React from 'react'
// import FilterBox from 'filterBox'
// import NewBox from 'newBox'
import Fias from 'fias'
//var FilterBox = require('./filterBox.jsx');
//var NewBox = require('./newBox.jsx');
//var Fias = require('./fias.jsx');
//var NavBar = require('./navBar.jsx');

export default class FiasList extends React.Component {
    constructor(props){
        super(props);
        this.fiasList = this.fiasList.bind(this);
        this.fiasNew = this.fiasNew.bind(this);
    }

    componentDidMount() {
        this.fiasList("");
    }

    fiasList(text) {
        fetch('http://77.244.208.236:5055/?q=' + text)
            .then(res => res.json())
            .then((data) => {
                this.setState({fias: data});
            })
            .catch(console.log)
    }

    fiasNew(text) {
        fetch('http://77.244.208.236:5055/add?q=' + text)
            .then(res => res.json())
            .then((data) => {
                this.fiasList("");
            })
            .catch(console.log)
    }

    render() {
        if (!this.state || !this.state.fias)
            return <h2>Loading...</h2>
        return(
            <div> <h2>Hello!</h2>
                <table>
                    <tr>
                        <td>
                            <h2>{"Fias History"}</h2>
                            <FilterBox filter={this.fiasList} />
                        </td>

                        <td>
                            <h2>{"Новый запрос"}</h2>
                            <NewBox click={this.fiasNew}/>
                        </td>
                    </tr>
                </table>

        <ul>
        {
            this.state.fias.map(function(fias){

                    return   <Fias fias={fias} />

            })
        }
        </ul>
        </div>);
    }
}
