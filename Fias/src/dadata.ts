import Fias, {IFias} from "./model/fias";
const fetch = require("node-fetch");

export class Dadata{
    private contentUrl: string
    private token: string
    private secret: string

    constructor(contentUrl: string, token: string, secret: string ) {
        this.contentUrl = contentUrl
        this.token = token
        this.secret = secret
    }

    async Look(query: string) {
       var options = {
           method: "POST",
           mode: "cors",
           headers: {
               "Content-Type": "application/json",
               "Authorization": "Token " + this.token,
               "X-Secret": this.secret
           },
           body: JSON.stringify([query])
       }

       let res = await fetch(this.contentUrl, options)
       let json = await res.json()

       let a = json[0];
       let result = new Fias();
       result.fias_code = a.fias_code;
       result.address = a.result;
       result.ipaddr = "127.0.0.1";
       result.time = new Date().toISOString();
       return result
   }
}
