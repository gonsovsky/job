import {Storage} from "./storage";
import {Dadata} from "./dadata";

import { addSwaggerDefinition, GET, POST, PUT, DELETE } from "mgr-swagger-express"
import express from 'express'
import * as bodyParser from 'body-parser'
import * as swaggerUI from 'swagger-ui-express'
import generateSwagger, { SET_EXPRESS_APP } from 'mgr-swagger-express'
const app = express()
SET_EXPRESS_APP(app)

type Appeal = {
    id: string
    address: string
    fias_code: string
    ipaddr: string
    time: string
}

type NewAppeal = {
    address: string
    fias_code: string
    ipaddr: string
}

const NewAppealDefinition = {
    type: "object",
    properties: {
        address: {
            type: "string"
        },
        fias_code: {
            type: "string"
        },
        ipaddr: {
            type: "string"
        },
    }
}

const AppealDefinition = {
    type: "object",
    properties: {
        id: {
            type: "string"
        },
        time: {
            type: "string"
        },
        address: {
            type: "string"
        },
        fias_code: {
            type: "string"
        },
        ipaddr: {
            type: "string"
        },
    }
}

app.use(bodyParser.urlencoded({ extended: true }))
app.use(bodyParser.json())

app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');

    res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept');
    next();

    app.options('*', (req, res) => {
        // allowed XHR methods
        res.header('Access-Control-Allow-Methods', 'GET, PATCH, PUT, POST, DELETE, OPTIONS');
        res.send();
    });
});


var path = require('path');
var x = path.join(__dirname, '..\\web\\dist');

let stat = express.static(x)
app.use('/demo', stat);

addSwaggerDefinition("Appeal", AppealDefinition)
addSwaggerDefinition("NewAppeal", NewAppealDefinition)

const swaggerDocument = generateSwagger({
    name: "Мобильное приложение (ФИАС)",
    version: "0.0.1",
    description: "API for the controller's office",
    host: `localhost:${5000}`,
    basePath: '/',
})

app.use(
    '/swagger',
    swaggerUI.serve,
    swaggerUI.setup(swaggerDocument));

const appealStore: { [id: string]: Appeal } = {}

export default class Service {

    private static storage: Storage
    private static dadata: Dadata
    private static service: Service

    constructor( astorage: Storage, adadata: Dadata, port: number) {
        Service.storage = astorage
        Service.dadata = adadata;
        Service.service = this;

        /*fix*/
        app.get('/', async function(req, res) {
            var query = require('url').parse(req.url,true).query;
            var filter = query.q || "";
            if (filter != "") {
                let response = await Service.service.getAppealsFiltered({filter}, null)
                res.send(response);
            } else
            {
                let response = await Service.service.getAppeals({}, null)
                res.send(response);
            }
        });
        app.get('/add', async function(req, res) {
            var query = require('url').parse(req.url, true).query;
            var filter = query.q || "";
            var newobj = await Service.dadata.Look(filter)
            var appeal = {
                address: filter,
                ipaddr: req.connection.remoteAddress,
                fias_code: newobj.fias_code,
                time: newobj.time,
                id: ""
            }
            let response = await Service.service.createNewAppeal({appeal}, null)
            res.send(response);
        });

        app.listen(port, () => {
            console.log(`Server started at port ${port}`)
        })
    }

    @GET({
        path: '/api/appeals',
        description: 'Получить все заявки',
        tags: ['Заявка в диспетчерскую'],
        success: '#/definitions/Appeal',
        parameters: [{
            name: 'filter',
            description: 'Необязательный фильтр',
        }],
    })
    public async getAppeals(args: any, context: any) {
        let data = await Service.storage.search("")
        return data;
    }

    @GET({
        path: '/api/appeals/:filter',
        description: 'Получить все заявки c фильтром',
        tags: ['Заявка в диспетчерскую'],
        success: '#/definitions/Appeal',
        parameters: [{
            name: 'filter',
            description: 'Фильтр',
        }],
    })
    public async getAppealsFiltered({ filter }: { filter: string }, context: any) {
        filter = filter || "";
        let data = await Service.storage.search(filter)
        return data;
    }

    @POST({
        path: '/api/appeals',
        description: 'Создать новую заявку',
        tags: ['Заявка в диспетчерскую'],
        success: '#/definitions/Appeal',
        body: {
            type: 'object',
            name: 'appeal',
            description: "Новая заявка",
            required: true,
            schema: {
                "$ref": '#/definitions/NewAppeal',
            }
        }
    })
    public async createNewAppeal({ appeal }: { appeal: Appeal }, context: any) {
        if (!appeal) {
            throw {
                status: 500,
                message: 'Ошибка создания заявки',
            }
        }
        if (appeal.fias_code == null || appeal.fias_code == undefined) {
            let newobj = await Service.dadata.Look(appeal.address)
            appeal.fias_code = newobj.fias_code
        }
        appeal.time = new Date().toISOString();
        await Service.storage.put(appeal)
        return appeal
    }

    @GET({
        path: '/api/appeal/:appeal_id',
        description: 'Получить одну заявку',
        parameters: [{
            name: 'appeal_id',
            description: 'ID заявки',
        }],
        tags: ['Заявка в диспетчерскую'],
        success: '#/definitions/Appeal',
    })
    public async getAppealById({ appeal_id }: { appeal_id: string }, context: any) {
        return await Service.storage.get(appeal_id)
            .then(
                (appeal: any) => {
                    if (appeal == null)
                    {
                        throw {
                            status: 404,
                            error: 'Заявка не найдена'
                        }
                    }
                    return appeal
                }
            )
            .catch((error) => {
                throw {
                    status: 404,
                    error: 'Заявка не найдена'
                }
            })
    }

    @PUT({
        path: '/api/appeal/:appeal_id',
        description: 'Обновить одну заявку',
        tags: ['Заявка в диспетчерскую'],
        success: '#/definitions/Appeal',
        parameters: [{
            name: 'appeal_id',
            description: 'ID заявки',
        }],
        body: {
            type: 'object',
            name: 'update',
            description: "Обновленная заявка",
            required: true,
            schema: {
                "$ref": '#/definitions/Appeal',
            }
        }
    })
    public async updateAppeal({ appeal_id, update }: { appeal_id: string, update: Appeal }, context: any) {
        let appeal = await Service.storage.get(appeal_id)
        if (!appeal){
            throw {
                status: 404,
                error: 'Заявка не найдена'
            }
        };
        await Service.storage.update(appeal_id, update)
        return await Service.storage.get(appeal_id)
    }

    @DELETE({
        path: '/api/appeal/:appeal_id',
        description: 'Удалить одну заявку',
        parameters: [{
            name: 'appeal_id',
            description: 'ID заявки',
        }],
        tags: ['Заявка в диспетчерскую'],
    })
    public async deleteAppeal({ appeal_id }: { appeal_id: string }, context: any) {
        let appeal = await Service.storage.get(appeal_id)
        if (!appeal){
            throw {
                status: 404,
                error: 'Заявка не найдена'
            }
        };
        return await Service.storage.del(appeal_id)
    }

}
