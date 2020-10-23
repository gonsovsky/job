import Fias , {IFias} from "./model/fias"
import mongoose = require("mongoose")

export class Storage  {
    private conStr: string;

    constructor(conStr: string) {
        this.conStr = conStr
        mongoose.connection.on('disconnected', this.connect);
        this.connect();
    }

    connect() {
        mongoose
            .connect(
                this.conStr, {useNewUrlParser: true}
            )
            .then(() => {
                return console.info(`Successfully connected to ${this.conStr}`)
            })
            .catch(error => {
                console.error('Error connecting to database: ', error)
            });
    };

    async put(fias: any) {
        await Fias.create(fias)
    }

    async get(id: string) {
        return await Fias.findById(id)
    }

    async update(id: string, fias: any) {
       return await Fias.findByIdAndUpdate(id,fias)
    }

    async del(id: string) {
        let result = await Fias.findByIdAndDelete(id)
        return result
    }

    async search(request: string) {
        request = request.trim()
        if (request == "")
            return await Fias.find().sort('-time')
        else {
            var r = new RegExp("" + request, 'i')
            var o = [{address: r}, {fias_code: r}]
            return await Fias.find().or(o).sort('-time')
        }
    }
}
