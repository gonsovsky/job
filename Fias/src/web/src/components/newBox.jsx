var React = require('react');

class NewBox extends React.Component{

    constructor(props){
        super(props);
        this.handleClick = this.handleClick.bind(this);
    }

    handleClick() {
        var txt = document.getElementById("newtext").value;
        this.props.click(txt);
    }

    render() {
        return <div>
            <input id="newtext" name="newtext" onChange={this.onTextChanged} placeholder="Новый запрос" />
            <button onClick={this.handleClick}>Запрос</button>
        </div> ;
    }
}

module.exports = NewBox;