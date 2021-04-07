import MenuService from "../../services/MenuService";
import AccountList from "./AccountList";

export default function addAccounts(ms: MenuService) {
    const g = ms.addGroup("Accounts", "fas fa-search");
    g.addTabLink("List", AccountList, null, "fas fa-users");
}
