import Bind from "@web-atoms/core/dist/core/Bind";
import {BindableProperty} from "@web-atoms/core/dist/core/BindableProperty";
import XNode from "@web-atoms/core/dist/core/XNode";
import {AtomItemsControl} from "@web-atoms/core/dist/web/controls/AtomItemsControl";
import MenuService from "../services/MenuService";
import MenuItemTemplate from "./MenuItemTemplate";

export default class MenuView extends AtomItemsControl {

	public create(): void {

		this.render(
		<div
			items={Bind.oneTime(() => this.resolve(MenuService).menus)}
			itemTemplate={Bind.oneTime(() => MenuItemTemplate)}>
		</div>
		);
	}
}
