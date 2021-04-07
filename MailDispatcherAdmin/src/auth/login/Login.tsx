import Bind from "@web-atoms/core/dist/core/Bind";
import XNode from "@web-atoms/core/dist/core/XNode";
import { AtomWindow } from "@web-atoms/core/dist/web/controls/AtomWindow";
import AtomField from "@web-atoms/web-controls/dist/form/AtomField";
import AtomForm from "@web-atoms/web-controls/dist/form/AtomForm";
import LoginViewModel from "./LoginViewModel";

export default class Login extends AtomWindow {

    public viewModel: LoginViewModel;

    public create() {
        this.viewModel = this.resolve(LoginViewModel);
        this.render(<AtomWindow>
            <AtomWindow.windowTemplate>
                <AtomForm>
                    <AtomField
                        label="Username:"
                        error={Bind.oneWay(() => this.viewModel.errorUsername)}>
                        <input value={Bind.twoWaysImmediate(() => this.viewModel.userName)}/>
                    </AtomField>
                    <AtomField
                        label="Password:"
                        error={Bind.oneWay(() => this.viewModel.errorPassword)}>
                        <input
                            type="password"
                            value={Bind.twoWaysImmediate(() => this.viewModel.password)}/>
                    </AtomField>
                </AtomForm>
            </AtomWindow.windowTemplate>
            <AtomWindow.commandTemplate>
                <div>
                    <button
                        eventClick={() => this.viewModel.login()}
                        text="Login"/>
                </div>
            </AtomWindow.commandTemplate>
        </AtomWindow>);
    }

}
