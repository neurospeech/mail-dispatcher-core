import Bind from "@web-atoms/core/dist/core/Bind";
import {BindableProperty} from "@web-atoms/core/dist/core/BindableProperty";
import XNode from "@web-atoms/core/dist/core/XNode";
import {AtomGridView} from "@web-atoms/core/dist/web/controls/AtomGridView";
import {AtomTabbedPage} from "@web-atoms/core/dist/web/controls/AtomTabbedPage";
import { DashboardTabbedStyle } from "../style/DashboardTabbedStyle";
import { TabHostStyle } from "../style/TabHostStyle";
import MenuView from "./MenuView";

export default class TabHost extends AtomGridView {

	public create(): void {
		this.defaultControlStyle = TabHostStyle;

		this.render(
		<div
			styleClass={Bind.oneTime(() => this.controlStyle.name)}
			columns="200,*"
			rows="40, *">
			<div
				row="0"
				column="0"
				class="heading-title">Mail Dispatcher</div>
			<div
				row="0"
				column="1"
				class="header2">
			</div>
			<MenuView
				row="1"
				column="0"
				class="menu-view"
				for="div">
			</MenuView>
			<AtomTabbedPage
				row="0:2"
				column="1"
				style="top: 10px !important"
				controlStyle={DashboardTabbedStyle}>
			</AtomTabbedPage>
		</div>
		);
	}
}
