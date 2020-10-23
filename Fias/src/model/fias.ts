import mongoose, { Schema, Document } from 'mongoose'

export interface IFias extends Document {
    address: string
    fias_code: string
    ipaddr: string
    time: string
}

const FiasSchema: Schema = new Schema({
    address: {type: String, required: false},
    fias_code: {type: String, required: false},
    ipaddr: {type: String, required: false},
    time: {type: String, required: false}
});

export default mongoose.model<IFias>('Fias', FiasSchema)