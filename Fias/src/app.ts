import {Dadata} from "./dadata"
import {Storage} from "./storage"
import Service from "./service";

//http://77.244.208.236:5056/
//http://77.244.208.236:5055/
// Запуск службы:  systemctl start mobile
// стоп службы:  systemctl stop mobile

let storage = new Storage("mongodb://192.168.100.184:27017/")

let dadata = new Dadata(
    "https://cleaner.dadata.ru/api/v1/clean/address",
    "49428f116b81e0fe672fcda687a1928a7f4bfc46",
    "bc8a91f5a2f8469e3d58e402c0f9c86d3bb59b0a",
);

let svc = new Service(storage, dadata, 5000)
