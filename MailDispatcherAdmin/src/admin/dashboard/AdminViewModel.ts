import { Inject } from "@web-atoms/core/dist/di/Inject";
import { NavigationService } from "@web-atoms/core/dist/services/NavigationService";
import { AtomViewModel } from "@web-atoms/core/dist/view-model/AtomViewModel";
import Load from "@web-atoms/core/dist/view-model/Load";
import AuthService from "../../services/AuthService";
import MenuService from "../../services/MenuService";

export default class AdminViewModel extends AtomViewModel {

    @Inject
    public readonly navigationService: NavigationService;

    @Inject
    public readonly menuService: MenuService;

    @Inject
    public readonly headerMenuService: MenuService;

    @Inject
    private authService: AuthService;

    @Load({ init: true })
    public async load() {
        await this.authService.getUser();
    }

}
