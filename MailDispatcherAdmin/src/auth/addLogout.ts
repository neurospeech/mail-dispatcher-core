import { NavigationService } from "@web-atoms/core/dist/services/NavigationService";
import AuthService from "../services/AuthService";
import MenuService from "../services/MenuService";
import ChangePassword from "./change-password/ChangePassword";

export default function addLogout(
    ms: MenuService,
    authService: AuthService,
    navigationService: NavigationService): void {

    const g = ms.addGroup("User", "fas fa-users");
    g.add("Change Password", async () => {
        await navigationService.openPage(ChangePassword, { title: "Change Password" });
    }, "fas fa-sign-out-alt");
    g.add("Logout", async () => {
        await authService.logout();
        location.hash = "";
        location.reload(true);
    }, "fas fa-sign-out-alt");

}
