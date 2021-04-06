import TabHost from "../../core/TabHost";
import AdminViewModel from "./AdminViewModel";
export default class Admin extends TabHost {

    public viewModel: AdminViewModel;

    public create() {
        this.viewModel = this.resolve(AdminViewModel);
        super.create();
    }

}
