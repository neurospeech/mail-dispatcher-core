import DISingleton from "@web-atoms/core/dist/di/DISingleton";
import BaseUrl, { BaseService, Get } from "@web-atoms/core/dist/services/http/RestService";

export interface IAccount {
    id?: string;
    authKey?: string;
    domainName?: string;
    selector?: string;
    publicKey?: string;
}

@DISingleton()
@BaseUrl("/api/accounts")
export default class AccountService extends BaseService {

    @Get("")
    public getList(): Promise<IAccount[]> {
        return null;
    }

}
