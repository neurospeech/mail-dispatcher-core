import Bind from "@web-atoms/core/dist/core/Bind";
import XNode from "@web-atoms/core/dist/core/XNode";
import { AtomGridView } from "@web-atoms/core/dist/web/controls/AtomGridView";
import { AtomItemsControl } from "@web-atoms/core/dist/web/controls/AtomItemsControl";
import AccountListViewModel from "./AccountListViewModel";
import { IAccount } from "./AccountService";

const BindAccount = Bind.forData<IAccount>();

export default class AccountList extends AtomGridView {

    public viewModel: AccountListViewModel;

    public create() {
        this.viewModel = this.resolve(AccountListViewModel);
        this.render(<AtomGridView
            rows="50,*">
            <div>
                <button
                    text="Add"/>
            </div>
            <div row="1">
                <AtomItemsControl for="table" items={Bind.oneWay(() => this.viewModel.model)}>
                    <AtomItemsControl.itemTemplate>
                        <tr>
                            <td>
                                <span text={BindAccount.oneWay((x) => x.data.id)}/>
                            </td>
                            <td>
                                <span text={BindAccount.oneWay((x) => x.data.domainName)}/>
                            </td>
                            <td>
                                <span text={BindAccount.oneWay((x) => x.data.selector)}/>
                            </td>
                        </tr>
                    </AtomItemsControl.itemTemplate>
                </AtomItemsControl>
            </div>
        </AtomGridView>);
    }
}
