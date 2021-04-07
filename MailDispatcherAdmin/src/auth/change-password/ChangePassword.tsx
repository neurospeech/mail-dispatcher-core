import Bind from "@web-atoms/core/dist/core/Bind";
import XNode from "@web-atoms/core/dist/core/XNode";
import { AtomWindow } from "@web-atoms/core/dist/web/controls/AtomWindow";
import AtomField from "@web-atoms/web-controls/dist/form/AtomField";
import AtomForm from "@web-atoms/web-controls/dist/form/AtomForm";
import ChangePasswordViewModel from "./ChangePasswordViewModel";

export default class ChangePassword extends AtomWindow {

    public viewModel: ChangePasswordViewModel;

    public create() {

        this.viewModel = this.resolve(ChangePasswordViewModel);

        this.render(<AtomWindow>
            <AtomWindow.windowTemplate>
                <AtomForm>
                    <AtomField
                        label="Old Password"
                        error={Bind.oneWay(() => this.viewModel.errorOldPassword)}>
                        <input
                            type="password"
                            value={Bind.twoWaysImmediate(() => this.viewModel.model.oldPassword)}
                            />
                    </AtomField>
                    <AtomField
                        label="New Password"
                        error={Bind.oneWay(() => this.viewModel.errorNewPassword)}>
                        <input
                            type="password"
                            value={Bind.twoWaysImmediate(() => this.viewModel.model.newPassword)}
                            />
                    </AtomField>
                    <AtomField
                        label="New Password (Again)"
                        error={Bind.oneWay(() => this.viewModel.errorNewPasswordAgain)}>
                        <input
                            type="password"
                            value={Bind.twoWaysImmediate(() => this.viewModel.model.newPasswordAgain)}
                            />
                    </AtomField>
                </AtomForm>
            </AtomWindow.windowTemplate>
            <AtomWindow.commandTemplate>
                <div>
                    <button
                        eventClick={Bind.event(() => this.viewModel.changePassword())}
                        text="Change"
                        />
                </div>
            </AtomWindow.commandTemplate>
        </AtomWindow>);
    }
}
