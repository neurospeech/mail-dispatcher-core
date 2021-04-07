import DISingleton from "@web-atoms/core/dist/di/DISingleton";
import { Inject } from "@web-atoms/core/dist/di/Inject";
import BaseUrl, {
    BaseService, Body, Delete, Get, Path, Post, Put,
    Query } from "@web-atoms/core/dist/services/http/RestService";
import { NavigationService } from "@web-atoms/core/dist/services/NavigationService";
import Login from "../auth/login/Login";

export interface IUser {
    id?: string;
}

export interface IPasswordModel {
    oldPassword?: string;
    newPassword?: string;
    newPasswordAgain?: string;
}

@DISingleton()
@BaseUrl("/api/auth/")
export default class AuthService extends BaseService {

    public showProgress = false;

    public showError = false;

    @Inject
    public navigationService: NavigationService;

    public async getUser(): Promise<IUser> {
        while (true) {
            try {
                return await this.remoteGetUser();
            } catch (e) {
                await this.navigationService.openPage(Login);
            }
        }
    }

    @Put("login")
    public login(@Body model: {
        username: string,
        password: string
    }): Promise<IUser> {
        return null;
    }

    @Post("password")
    public changePassword(@Body model: IPasswordModel) {
        return null;
    }

    @Delete("")
    public logout() {
        return null;
    }

    @Get("")
    private remoteGetUser(): Promise<IUser> {
        return null;
    }

}
