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
        <button onMouseEnter={onMouseEnter} onMouseLeave={onMouseLeave} onClick={onClick} className={"button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT toggle-states_X82 toggle-states_DTm" + (model.IsVisible ? " selected" : "")}>

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
    const [expanded, setExpanded] = react.useState(false);

    const subFitlers = {
        "Zones": ["ZoneResidential", "ZoneCommercial", "ZoneIndustrial", "ZoneOffice"],
        "Buildings": ["ServiceBuilding", "SignatureBuilding"],
        "Misc": ["Vehicle", "Prop"],
        "Foliage": ["Tree", "Plant"],
    };

    const checkFilterTypes = (p) => {
        const hasFilter = model.Filter && model.Filter.length > 0 && model.Filter !== "None";
        const hasSubFilter = model.SubFilter && model.SubFilter.length > 0 && model.SubFilter !== "None";

        return (hasFilter && model.Filter === "Favourite" ? model.Favourites && model.Favourites.includes(p.Name) : true ) && (hasSubFilter ? p.Type === model.SubFilter : true ) &&
            (hasFilter && model.Filter !== "Favourite" ? p.Type === model.Filter || subFitlers[model.Filter] && subFitlers[model.Filter].includes(p.Type) : true);
    };

    const updateSearchFilter = () => {
        let filtered = ((!model.Filter || model.Filter.length == 0 || model.Filter === "None") && (!search || search === "") ? model.Prefabs :
            model.Prefabs.filter(function (p) {
                return checkFilterTypes(p) &&
                    (search && search.length > 0 ?
                    (p.Name && prefabName(p).toLowerCase().includes(search.toLowerCase()) || p.Type && p.Type.toLowerCase().includes(search.toLowerCase()))
                    : true);
            })
        );

        filtered.sort((a, b) => prefabName(a).toLowerCase().localeCompare(prefabName(b).toLowerCase()));

        if (!model.OrderByAscending)
            filtered.reverse();

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
        trigger("OnSelectPrefab", prefab.Name);
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
                <b className={selectedPrefab.Name == text || prefabName(selectedPrefab) == text ? "text-dark bg-warning" : "text-dark bg-warning"}>{part}</b>
            </span> : part
        );
    };

    const prefabName = (p) => {
        const key = `Assets.NAME[${p.Name}]`;
        const name = _L(key);

        if (name === key)
            return p.Name;

        else return name;
    };

    const updateFilter = (filter) => {
        update("Filter", filter);
        if (model.SubFilter && subFitlers[filter] && !subFitlers[filter].includes(model.SubFilter)) {
            model.SubFilter = "None";
            update("SubFilter", "None");
        }
    };

    const favourteClick = (e, p) => {
        e.stopPropagation();

        if (!p)
            return;
        trigger("OnToggleFavourite", p.Name);
    };

    const renderItemContent = (p) => {
        const isFAIcon = p.TypeIcon.includes("fa:");
        const iconSrc = isFAIcon ? p.TypeIcon.replaceAll("fa:", "") : p.TypeIcon;
        const isFavourite = model.Favourites && model.Favourites.includes(p.Name);

        const renderFavourite = () => {
            if (model.ViewMode === "Columns" || model.ViewMode === "Rows") {
                return <Button circular icon style="trans-faded" onClick={(e) => favourteClick(e, p)} elementStyle={{ transform: 'scale(0.75)' }}>
                    <Icon icon={(isFavourite ? "solid-star" : "star")} className={(isFavourite ? "bg-secondary" : "bg-secondary")} fa />
                </Button>;
            }
            return <div className="p-absolute p-top-0 p-left-0 w-100 h-100">
                <Button className="p-absolute p-right-0 p-top-0 mr-2 mb-2" circular icon style="trans-faded" onClick={(e) => favourteClick(e, p)} elementStyle={{ transform: 'scale(0.75)', ...(model.ViewMode === "Detailed" ? { marginTop: '-2.5rem'} : null )}} >
                    <Icon icon={(isFavourite ? "solid-star" : "star")} className={(isFavourite ? "bg-secondary" : "bg-secondary")} fa />
                </Button>
            </div>;
        };

        return model.ViewMode == "Detailed" ? <>
            <Grid>
                <div className="col-7">
                    <div className="d-flex flex-row">
                        <img className="icon icon-sm ml-1 mr-1" src={p.Thumbnail} />
                        <span className="fs-sm flex-1">{highlightSearchTerm(prefabName(p), search)}</span>
                    </div>
                </div>
                <div className="col-2">
                    <span className="fs-xs h-x">
                        <Icon icon={iconSrc} fa={isFAIcon ? true : null} size="sm" className={(isFAIcon ? "bg-muted " : "") + "mr-1"} style={{ maxHeight: "16rem" }} />
                        {highlightSearchTerm(p.Type, search)}
                    </span>
                </div>
                <div className="col-2">
                    {p.Meta && p.Meta.IsDangerous ? <div className="badge badge-xs badge-danger">Dangerous</div> : null}
                </div>
                <div className="col-1 p-relative pr-2">
                    {hoverPrefab && p.Name == hoverPrefab.Name ? renderFavourite() : null}
                </div>
            </Grid>
        </> : <>            
            <img className={model.ViewMode === "IconGrid" ? "icon icon-lg" : model.ViewMode === "IconGridLarge" ? "icon icon-xl" : "icon icon-sm ml-2"} src={p.Thumbnail} />
                {model.ViewMode === "Rows" || model.ViewMode === "Columns" ? <span className="ml-1 fs-sm mr-4">{highlightSearchTerm(prefabName(p), search)}</span> : <span className="fs-xs ml-1 mr-4" style={{ maxWidth: '80%', textOverflow: 'ellipsis', overflowX: 'hidden' }}>{highlightSearchTerm(prefabName(p), search)}</span>}
                {hoverPrefab && p.Name == hoverPrefab.Name ? renderFavourite() : null}
        </>;
    };

    const onRenderItem = (p, index) => {
        const borderClass = model.Filter !== "Favourite" && model.Favourites.includes(p.Name) ? " border-secondary-trans" : p.Meta && p.Meta.IsDangerous ? " border-danger-trans" : "";
        return <Button color={selectedPrefab.Name == p.Name ? "primary" : "light"} style={selectedPrefab.Name == p.Name ? "trans" : "trans-faded"} onMouseEnter={() => onMouseEnter(p)} className={"asset-menu-item auto flex-1 m-mini" + borderClass + (selectedPrefab.Name == p.Name ? " text-dark" : " text-light") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" ? " flat" : "") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" && selectedPrefab.Name !== p.Name ? " btn-transparent" : "")} onClick={() => onSelectPrefab(p)}>
            <div className={"d-flex align-items-center justify-content-center p-relative " + (model.ViewMode === "Columns" || model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? " w-x flex-row " : " flex-column")}>
                {renderItemContent(p)}
            </div>
        </Button>;
    };

    const renderSubOptions = () => {
        const subOptionsHeader = <>
            <h5 className="mr-2 text-muted">{model.Filter}</h5>
            <Button className={(!model.SubFilter || model.SubFilter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "None")}>
                <Icon icon="solid-asterisk" fa />
            </Button>
        </>;

        if (model.Filter === "Zones") {
            return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
                {subOptionsHeader}
                <Button className={"ml-1" + (model.SubFilter === "ZoneResidential" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "ZoneResidential")}>
                    <Icon icon="Media/Game/Icons/ZoneResidential.svg" />
                </Button>
                <Button className={"ml-1" + (model.SubFilter === "ZoneCommercial" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "ZoneCommercial")}>
                    <Icon icon="Media/Game/Icons/ZoneCommercial.svg" />
                </Button>
                <Button className={"ml-1" + (model.SubFilter === "ZoneOffice" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "ZoneOffice")}>
                    <Icon icon="Media/Game/Icons/ZoneOffice.svg" />
                </Button>
                <Button className={"ml-1 mr-1" + (model.SubFilter === "ZoneIndustrial" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "ZoneIndustrial")}>
                    <Icon icon="Media/Game/Icons/ZoneIndustrial.svg" />
                </Button>
            </div>;
        }
        else if (model.Filter === "Buildings") {
            return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
                {subOptionsHeader}
                <Button className={"ml-1" + (model.SubFilter === "ServiceBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "ServiceBuilding")}>
                    <Icon icon="Media/Game/Icons/Services.svg" />
                </Button>
                <Button className={"ml-1" + (model.SubFilter === "SignatureBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "SignatureBuilding")}>
                    <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
                </Button>
            </div>;
        }
        else if (model.Filter === "Foliage") {
            return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
                {subOptionsHeader}
                <Button className={"ml-1" + (model.SubFilter === "Tree" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "Tree")}>
                    <Icon icon="Media/Game/Icons/Forest.svg" />
                </Button>
                <Button className={"ml-1" + (model.SubFilter === "Plant" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "Plant")}>
                    <Icon icon="Media/Game/Icons/Forest.svg" />
                </Button>
            </div>;
        }
        else if (model.Filter === "Misc") {
            return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
                {subOptionsHeader}
                <Button className={"ml-1" + (model.SubFilter === "Vehicle" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "Vehicle")}>
                    <Icon icon="Media/Game/Icons/Traffic.svg" />
                </Button>
                <Button className={"ml-1" + (model.SubFilter === "Prop" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("SubFilter", "Prop")}>
                    <Icon icon="solid-cube" fa />
                </Button>
            </div>;
        }

        return null;
    };

    const prefabDesc = (p) => {
        const key = `Assets.DESCRIPTION[${p.Name}]`;
        const trans = _L(key);

        if (trans === key)
            return null;

        return trans;
    };

    const renderHoverContents = () => {
        if (!hoverPrefab)
            return;

        const prefabDescText = prefabDesc(hoverPrefab);

        return <Grid>
            <div className="col-3">
                <Icon icon={hoverPrefab.Thumbnail} size="xxl" />
            </div>
            <div className="col-9">
                {prefabDescText ?
                    <p className="mb-4 fs-sm" cohinline="cohinline">
                        {prefabDescText}
                    </p> : null }
                {hoverPrefab.Meta && hoverPrefab.Meta.IsDangerous ? <div className="alert alert-danger fs-sm d-flex flex-row flex-wrap align-items-center p-2 mb-4">
                    <Icon className="mr-2" icon="solid-triangle-exclamation" fa />
                    {hoverPrefab.Meta.IsDangerousReason}
                </div> : null}
                <div className="d-inline">
                    {hoverPrefab.Tags.map((tag, index) => <div key={index} className="badge badge-info">
                        {tag}
                    </div>)}
                </div>
            </div>
        </Grid>
    };

    const modalTypeIconIsFAIcon = hoverPrefab && hoverPrefab.TypeIcon ? hoverPrefab.TypeIcon.includes("fa:") : false;
    const modalTypeIconSrc = modalTypeIconIsFAIcon ? hoverPrefab.TypeIcon.replaceAll("fa:", "") : hoverPrefab ? hoverPrefab.TypeIcon : null;

    return model.IsVisible ? <div className={isVisibleClass}>
        <div className="col">
            <div className="bg-panel text-light p-4 rounded-sm">
                <div className="d-flex flex-row align-items-center justify-content-center">
                    <div className="flex-1">
                        {_L("FindStuff.View")}
                    </div>
                    <Button className={"mr-1" + (model.ViewMode === "Rows" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Rows")}>
                        <Icon icon="solid-bars" fa />
                    </Button>
                    <Button className={"mr-1" + (model.ViewMode === "Columns" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Columns")}>
                        <Icon icon="solid-table-columns" fa />
                    </Button>
                    <Button className={"mr-1" + (model.ViewMode === "IconGrid" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGrid")}>
                        <Icon icon="solid-table-cells" fa />
                    </Button>
                    <Button className={"mr-1" + (model.ViewMode === "IconGridLarge" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGridLarge")}>
                        <Icon icon="solid-table-cells-large" fa />
                    </Button>
                    <Button className={"" + (model.ViewMode === "Detailed" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Detailed")}>
                        <Icon icon="solid-table-list" fa />
                    </Button>
                </div>
                <div className="d-flex flex-row align-items-center justify-content-center mt-4">
                    <div className="flex-1">
                        {_L("FindStuff.Filter")}
                    </div>
                    <div>
                        <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                            <Button className={(!model.Filter || model.Filter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("None")}>
                                <Icon icon="solid-asterisk" fa />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Favourite" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Favourite")}>
                                <Icon icon="solid-star" fa />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Foliage" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Foliage")}>
                                <Icon icon="Media/Game/Icons/Forest.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Network" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Network")}>
                                <Icon icon="Media/Game/Icons/Roads.svg" />
                            </Button>
                        </div>
                        <div className="d-flex flex-row flex-wrap align-items-center justify-content-end mt-1">
                            <Button className={(model.Filter === "Buildings" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Buildings")}>
                                <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Zones" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Zones")}>
                                <Icon icon="Media/Game/Icons/Zones.svg" />                                
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Surface" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Surface")}>
                                <Icon icon="Media/Game/Icons/LotTool.svg" />
                            </Button>
                            <Button className={"ml-1" + (model.Filter === "Misc" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Misc")}>
                                <Icon icon="solid-ellipsis" fa />
                            </Button>
                        </div>
                    </div>
                </div>
                <div className="d-flex flex-row align-items-center justify-content-center mt-4">
                    <div className="flex-1">
                        {_L("FindStuff.OrderBy")}
                    </div>
                    <div>
                        <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                            <Button className={(model.OrderByAscending === true ? " active" : "")} color="tool" size="sm" icon onClick={() => update("OrderByAscending", true)}>
                                <Icon icon="solid-arrow-down-a-z" fa />
                            </Button>
                            <Button className={"ml-1" + (model.OrderByAscending === false ? " active" : "")} color="tool" size="sm" icon onClick={() => update("OrderByAscending", false)}>
                                <Icon icon="solid-arrow-up-a-z" fa />
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div className="col">
            {hoverPrefab && hoverPrefab.Name.length > 0 ?
                <Modal className="mb-2" icon={<><Icon icon={modalTypeIconSrc} fa={modalTypeIconIsFAIcon ? true : null} /></>} title={prefabName(hoverPrefab)} noClose>
                    {renderHoverContents()}
                </Modal> : null }
            <Modal bodyClassName={"asset-menu" + (expanded ? " asset-menu-xl" : "")} title={<div className="d-flex flex-row align-items-center">
                <Button circular icon style="trans-faded" onClick={() => setExpanded(!expanded)}>
                    <Icon icon={expanded ? "solid-chevron-down" : "solid-chevron-up"} fa />
                </Button>
                <Icon icon="solid-magnifying-glass" fa className="bg-muted ml-2" />
                <TextBox size="sm" className="bg-dark-trans-less-faded w-25 mr-2 ml-4" placeholder="Search..." text={search} onChange={onSearchInputChanged} />
                {<Button circular icon style="trans-faded" disabled={search && search.length > 0 ? null : true} onClick={() => setSearch("")}>
                    <Icon icon="solid-eraser" fa />
                </Button>}
                {renderSubOptions()}
            </div>} onClose={closeModal}>
                <div className="asset-menu-container" onMouseLeave={() => onMouseLeave()}>
                    <div className="flex-1">
                        <VirtualList border={model.ViewMode === "IconGrid" || model.ViewMode === "IconGridLarge" ? null : true} data={filteredPrefabs} onRenderItem={onRenderItem} columns={model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? 1 : model.ViewMode === "Columns" ? 2 : model.ViewMode === "IconGrid" ? 13 : 9} rows={model.ViewMode === "Rows" || model.ViewMode === "Detailed" || model.ViewMode === "Columns" ? (expanded ? 8 : 4) : model.ViewMode === "IconGrid" ? (expanded ? 6 : 3) : ( expanded ? 4 : 2 )} contentClassName="d-flex flex-row flex-wrap" size="sm" itemHeight={32}>
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
