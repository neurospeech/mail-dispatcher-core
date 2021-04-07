import { Inject } from "@web-atoms/core/dist/di/Inject";
import Action from "@web-atoms/core/dist/view-model/Action";
import { Validate } from "@web-atoms/core/dist/view-model/AtomViewModel";
import { AtomWindowViewModel } from "@web-atoms/core/dist/view-model/AtomWindowViewModel";
import AuthService, { IPasswordModel } from "../../services/AuthService";

export default class ChangePasswordViewModel extends AtomWindowViewModel {

    public model: IPasswordModel = {};

    @Validate
    public get errorOldPassword() {
        return this.model.oldPassword ? null : "Old password is required";
    }

    @Validate
    public get errorNewPassword() {
        const p = this.model.newPasswordAgain;
        if (!p) {
            return "New password is required";
        }
        if (p === this.model.oldPassword) {
            return "New password and old password cannot be same";
        }
        if (p.length < 8) {
            return "New password cannot be less than 8 characters";
        }
        return null;
    }

    @Validate
    public get errorNewPasswordAgain() {
        return this.model.newPassword
            ? (this.model.newPassword !== this.model.newPasswordAgain
                ? "Passwords do not match"
                : null)
            : "Old password is required";
    }

    @Inject
    private authService: AuthService;

    @Action({
        validate: true,
        success: "Password changed successfully"
    })
    public async changePassword() {
        await this.authService.changePassword(this.model);
        this.close();
    }
}
