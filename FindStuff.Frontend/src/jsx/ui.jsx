import React from "react";
import { Virtuoso } from "react-virtuoso";

const AppButton = ({ react, setupController }) => {
    const [tooltipVisible, setTooltipVisible] = react.useState(false);

    const onMouseEnter = () => {
        setTooltipVisible(true);
        engine.trigger("audio.playSound", "hover-item", 1);
    };

    const onMouseLeave = () => {
        setTooltipVisible(false);
    };

    const { ToolTip, ToolTipContent } = window.$_gooee.framework;

    const { model, update, trigger } = setupController();

    const onClick = () => {
        const newValue = !model.IsVisible;
        trigger("OnToggleVisible");
        engine.trigger("audio.playSound", "select-item", 1);

        if (newValue) {
            engine.trigger("audio.playSound", "open-panel", 1);
            //engine.trigger("tool.selectTool", null);
        }
        else
            engine.trigger("audio.playSound", "close-panel", 1);
    };
    return <>
        <div className="spacer_oEi"></div>
        <button onMouseEnter={onMouseEnter} onMouseLeave={onMouseLeave} onClick={onClick} class="button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT toggle-states_X82 toggle-states_DTm">

            <div className="fa fa-solid-magnifying-glass icon-lg"></div>

            <ToolTip visible={tooltipVisible} float="up" align="right">
                <ToolTipContent title="Test" description="Hello, world!" />
            </ToolTip>
        </button>
    </>;
};

window.$_gooee.register("findstuff", "FindStuffAppButton", AppButton, "bottom-right-toolbar", "findstuff");

const ToolWindow = ({ react, setupController }) => {
    const [sliderValue, setSliderValue] = react.useState(0);
    const [selectedPrefab, setSeletedPrefab] = react.useState({ Name: "" });
    const [hoverPrefab, setHoverPrefab] = react.useState({ Name: "" });
    const [tm, setTm] = react.useState(null);

    const { Button, Icon, VirtualList, Slider, List, Grid, FormGroup, FormCheckBox, Scrollable, ToolTip, TextBox, Dropdown, ToolTipContent, TabModal, Modal, MarkDown } = window.$_gooee.framework;

    const { model, update, trigger, _L } = setupController();
    const [filteredPrefabs, setFilteredPrefabs] = react.useState(model.Prefabs);
    const [search, setSearch] = react.useState("");
    
    const updateSearchFilter = () => {
        const filtered = ((!model.Filter || model.Filter.length == 0 || model.Filter === "None") && (!search || search === "") ? model.Prefabs :
            model.Prefabs.filter(function (p) {
                return (model.Filter && model.Filter.length > 0 && model.Filter !== "None" ? p.Type === model.Filter : true) &&
                    (search && search.length > 0 ?
                        (p.Name && p.Name.toLowerCase().includes(search.toLowerCase()) || p.Type && p.Type.toLowerCase().includes(search.toLowerCase()))
                    : true);
            })
        );
        setFilteredPrefabs(filtered);
    };

    react.useEffect(() => {
        updateSearchFilter();
    }, [model, search]);

    const onSearchInputChanged = (val) => {
        setSearch(val);
    };


    const closeModal = () => {
        trigger("OnToggleVisible");
        engine.trigger("audio.playSound", "close-panel", 1);
    };

    const isVisibleClass = "tool-layout";

    const onSelectPrefab = (prefab) => {
        setSeletedPrefab(prefab);
    };

    const onMouseEnter = (prefab) => {
        if (tm)
            clearTimeout(tm);
        setHoverPrefab(prefab);
    };

    const onMouseLeave = () => {
        setHoverPrefab(null);
    }
    
    const highlightSearchTerm = (text, searchTerm) => {
        const regex = new RegExp(`(${searchTerm})`, 'gi');
        const splitText = text.split(regex);

        return (!searchTerm || searchTerm.length == 0) ? text : splitText.map((part, index) =>
            regex.test(part) ? <span key={index}>
                <b className={selectedPrefab.Name == text ? "text-dark bg-warning" : "text-dark bg-warning"}>{part}</b>
            </span> : part
        );
    };

    const renderItemContent = (p) => {
        return model.ViewMode == "Detailed" ? <>
            <Grid>
                <div className="col-5">
                    <div className="d-flex flex-row">
                        <img className="icon icon-sm ml-1 mr-1" src={p.Thumbnail} />
                        <span className="fs-sm flex-1">{highlightSearchTerm(_L(`Assets.NAME[${p.Name}]`), search)}</span>
                    </div>
                </div>
                <div className="col-2">
                    <span className="fs-xs h-x">{highlightSearchTerm(p.Type, search)}</span>
                </div>
                <div className="col-5">
                </div>
            </Grid>
        </> : <>            
            <img className={model.ViewMode === "IconGrid" ? "icon icon-lg" : model.ViewMode === "IconGridLarge" ? "icon icon-xl" : "icon icon-sm ml-2"} src={p.Thumbnail} />
                {model.ViewMode === "Rows" || model.ViewMode === "Columns" ? <span className="ml-1 fs-sm mr-4">{highlightSearchTerm(_L(`Assets.NAME[${p.Name}]`), search)}</span> : <span className="fs-xs ml-1 mr-4" style={{ maxWidth: '80%', textOverflow: 'ellipsis', overflowX: 'hidden' }}>{highlightSearchTerm(_L(`Assets.NAME[${p.Name}]`), search)}</span>}
        </>;
    };

    const onRenderItem = (p, index) => {
        return <Button color={selectedPrefab.Name == p.Name ? "primary" : "light"} style={selectedPrefab.Name == p.Name ? "trans" : "trans-faded"} onMouseEnter={() => onMouseEnter(p)} className={"asset-menu-item auto flex-1 m-mini" + (selectedPrefab.Name == p.Name ? " text-dark" : " text-light") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" ? " flat" : "") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" && selectedPrefab.Name !== p.Name ? " btn-transparent" : "")} onClick={() => onSelectPrefab(p)}>
            <div className={"d-flex align-items-center justify-content-center " + (model.ViewMode === "Columns" || model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? " w-x flex-row " : " flex-column")}>
                {renderItemContent(p)}
            </div>
        </Button>;
    };

    return model.IsVisible ? <div className={isVisibleClass}>
        <div className="col">
            <div className="bg-panel text-light p-4 rounded-sm">
                <div className="d-flex flex-row align-items-center justify-content-center">
                    <div className="flex-1">
                        {_L("FindStuff.View")}
                    </div>
                    <Button className={"mr-1" + (model.ViewMode === "Rows" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Rows")}>
                        <Icon icon="solid-list" fa />
                    </Button>
                    <Button className={"mr-1" + (model.ViewMode === "Columns" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Columns")}>
                        <Icon icon="solid-list" fa />
                    </Button>
                    <Button className={"mr-1" + (model.ViewMode === "IconGrid" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGrid")}>
                        <Icon icon="solid-table-cells" fa />
                    </Button>
                    <Button className={"mr-1" + (model.ViewMode === "IconGridLarge" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGridLarge")}>
                        <Icon icon="solid-border-all" fa />
                    </Button>
                    <Button className={"" + (model.ViewMode === "Detailed" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Detailed")}>
                        <Icon icon="solid-align-justify" fa />
                    </Button>
                </div>
                <div className="d-flex flex-row align-items-center justify-content-center mt-4">
                    <div className="flex-1">
                        {_L("FindStuff.Filter")}
                    </div>
                    <div>
                        <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                            <Button className={(model.Filter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "None")}>
                                <Icon icon="solid-asterisk" fa />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Foliage" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "Foliage")}>
                                <Icon icon="Media/Game/Icons/Forest.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Network" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "Network")}>
                                <Icon icon="Media/Game/Icons/Roads.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "ServiceBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "ServiceBuilding")}>
                                <Icon icon="Media/Game/Icons/Services.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Signature" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "Signature")}>
                                <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "ZoneResidential" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "ZoneResidential")}>
                                <Icon icon="Media/Game/Icons/ZoneResidential.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "ZoneCommercial" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "ZoneCommercial")}>
                                <Icon icon="Media/Game/Icons/ZoneCommercial.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Vehicle" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "Vehicle")}>
                                <Icon icon="Media/Game/Icons/Traffic.svg" />
                            </Button>
                        </div>
                        <div className="d-flex flex-row flex-wrap align-items-center justify-content-end mt-1">
                            <Button className={"ml-1" + (model.Filter === "ZoneIndustrial" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("Filter", "ZoneIndustrial")}>
                                <Icon icon="Media/Game/Icons/ZoneIndustrial.svg" />
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div className="col">
            {hoverPrefab && hoverPrefab.Name.length > 0 ?
                <Modal className="mb-2" title={_L(`Assets.NAME[${hoverPrefab.Name}]`) } noClose>
                    <Icon icon={hoverPrefab.Thumbnail} size="xxl" />
                    <Icon icon={hoverPrefab.TypeIcon} size="xxl" />
                </Modal> : null }
            <Modal bodyClassName="asset-menu" title={<div className="d-flex flex-row align-items-center">
                <Icon icon="solid-magnifying-glass" fa className="bg-muted" />
                <TextBox size="sm" className="bg-dark-trans-less-faded w-50 mr-2 ml-4" placeholder="Search..." text={search} onChange={onSearchInputChanged} />
                {search && search.length > 0 ? <Button circular icon style="trans-faded" onClick={() => setSearch("")}>
                    <Icon icon="solid-eraser" fa />
                </Button> : null}
            </div>} onClose={closeModal}>
                <div className="asset-menu-container" onMouseLeave={() => onMouseLeave()}>
                    <div className="flex-1">
                        <VirtualList border={model.ViewMode === "IconGrid" || model.ViewMode === "IconGridLarge" ? null : true} data={filteredPrefabs} onRenderItem={onRenderItem} columns={model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? 1 : model.ViewMode === "Columns" ? 2 : model.ViewMode === "IconGrid" ? 13 : 9} rows={model.ViewMode === "Rows" || model.ViewMode === "Detailed" || model.ViewMode === "Columns" ? 4 : model.ViewMode === "IconGrid" ? 3 : 2} contentClassName="d-flex flex-row flex-wrap" size="sm" itemHeight={32}>
                        </VirtualList>
                    </div>
                </div>
            </Modal>
        </div>
        <div className="col">
        </div>
    </div> : null;
};

window.$_gooee.register("findstuff", "FindStuffWindow", ToolWindow, "main-container", "findstuff");
