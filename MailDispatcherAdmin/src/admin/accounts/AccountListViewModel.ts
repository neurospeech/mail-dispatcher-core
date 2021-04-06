import { Inject } from "@web-atoms/core/dist/di/Inject";
import { AtomViewModel } from "@web-atoms/core/dist/view-model/AtomViewModel";
import Load from "@web-atoms/core/dist/view-model/Load";
import AccountService, { IAccount } from "./AccountService";

export default class AccountListViewModel extends AtomViewModel {

    public model: IAccount[];

    @Inject
    private accountService: AccountService;

    @Load({ init: true, watch: true })
    public async loadAccounts() {
        this.model = await this.accountService.getList();
    }

}
