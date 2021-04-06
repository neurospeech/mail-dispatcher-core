import { Inject } from "@web-atoms/core/dist/di/Inject";
import Action from "@web-atoms/core/dist/view-model/Action";
import { Validate } from "@web-atoms/core/dist/view-model/AtomViewModel";
import { AtomWindowViewModel } from "@web-atoms/core/dist/view-model/AtomWindowViewModel";
import AuthService from "../services/AuthService";

export default class LoginViewModel extends AtomWindowViewModel {

    public userName: string = "";

    public password: string = "";

    @Validate
    public get errorUsername() {
        return this.userName ? "" : "Username is required";
    }

    @Validate
    public get errorPassword() {
        return this.password ? "" : "Password is required";
    }

    @Inject
    private authService: AuthService;

    @Action({
        validate: true
    })
    public async login(): Promise<void> {
        await this.authService.login({ username: this.userName, password: this.password });
        this.close();
    }
}
