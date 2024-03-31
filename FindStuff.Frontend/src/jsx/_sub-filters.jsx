import React from "react";

const SubFilters = ({ model, update, onDoUpdate, _L }) => {
    const react = window.$_gooee.react;
    const { Icon, Button } = window.$_gooee.framework;    

    const isVertical = model.OperationMode === "HideFindStuffSideMenu";

    const updateBackend = (val) => {
        model.SubFilter = val;
        update("SubFilter", val);

        if (onDoUpdate)
            onDoUpdate(model, false);
    };

    const categoryName = () => {
        const key = `FindStuff.PrefabCategory.${model.Filter}`;
        const name = _L(key);

        if (name === key)
            return model.Filter;
        else
            return name;
    };

    const computedCategoryName = react.useMemo(() => categoryName(), [model.Filter, _L]);

    const subOptionsHeader =/* react.useMemo(() => (*/
        <>
            <h5 className="mr-2 text-muted">{computedCategoryName}</h5>
            <Button title={_L("FindStuff.SubFilter.None")} description={_L("FindStuff.SubFilter.None_desc")}
                toolTipFloat={isVertical ? "down" : "up"}
                className={(!model.SubFilter || model.SubFilter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("None")}>
                <Icon icon="solid-asterisk" fa />
            </Button>
        </>
 /*   ), [model.Filter, model.SubFilter, update]);*/

    const containerClassName = "d-flex flex-row flex-wrap justify-content-end " + (isVertical ? " w-x bg-section-dark p-2 pt-0" : " mr-6 flex-1");

    if (model.Filter === "Zones") {
        return <div className={containerClassName}>
            {subOptionsHeader}
            <Button title={_L("FindStuff.SubFilter.ZoneResidential")} description={_L("FindStuff.SubFilter.ZoneResidential_desc")}
                toolTipFloat={isVertical ? "down" : "up"}
                className={"ml-1" + (model.SubFilter === "ZoneResidential" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneResidential")}>
                <Icon icon="Media/Game/Icons/ZoneResidential.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.ZoneCommercial")} description={_L("FindStuff.SubFilter.ZoneCommercial_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "ZoneCommercial" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneCommercial")}>
                <Icon icon="Media/Game/Icons/ZoneCommercial.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.ZoneOffice")} description={_L("FindStuff.SubFilter.ZoneOffice_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "ZoneOffice" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneOffice")}>
                <Icon icon="Media/Game/Icons/ZoneOffice.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.ZoneIndustrial")} description={_L("FindStuff.SubFilter.ZoneIndustrial_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1 mr-1" + (model.SubFilter === "ZoneIndustrial" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneIndustrial")}>
                <Icon icon="Media/Game/Icons/ZoneIndustrial.svg" />
            </Button>
        </div>;
    }
    else if (model.Filter === "Buildings") {
        return <div className={containerClassName}>
            {subOptionsHeader}
            <Button title={_L("FindStuff.SubFilter.ServiceBuilding")} description={_L("FindStuff.SubFilter.ServiceBuilding_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "ServiceBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ServiceBuilding")}>
                <Icon icon="Media/Game/Icons/Services.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.SignatureBuilding")} description={_L("FindStuff.SubFilter.SignatureBuilding_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "SignatureBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("SignatureBuilding")}>
                <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Park")} description={_L("FindStuff.SubFilter.Park_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Park" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Park")}>
                <Icon icon="Media/Game/Icons/ParksAndRecreation.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Parking")} description={_L("FindStuff.SubFilter.Parking_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Parking" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Parking")}>
                <Icon icon="Media/Game/Icons/Parking.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.MiscBuilding")} description={_L("FindStuff.SubFilter.MiscBuilding_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "MiscBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("MiscBuilding")}>
                <Icon icon="Media/Game/Icons/BuildingLevel.svg" />
            </Button>
        </div>;
    }
    else if (model.Filter === "Foliage") {
        return <div className={containerClassName}>
            {subOptionsHeader}
            <Button title={_L("FindStuff.SubFilter.Tree")} description={_L("FindStuff.SubFilter.Tree_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Tree" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Tree")}>
                <Icon icon="Media/Game/Icons/Forest.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Plant")} description={_L("FindStuff.SubFilter.Plant_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Plant" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Plant")}>
                <Icon icon="Media/Game/Icons/Forest.svg" />
            </Button>
        </div>;
    }
    else if (model.Filter === "Props") {
        return <div className={containerClassName}>
            {subOptionsHeader}
            <Button title={_L("FindStuff.SubFilter.Billboards")} description={_L("FindStuff.SubFilter.Billboards_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Billboards" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Billboards")}>
                <Icon icon="solid-rectangle-ad" fa />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Fences")} description={_L("FindStuff.SubFilter.Fences_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Fences" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Fences")}>
                <Icon icon="solid-xmarks-lines" fa />
            </Button>
            <Button title={_L("FindStuff.SubFilter.SignsAndPosters")} description={_L("FindStuff.SubFilter.SignsAndPosters_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "SignsAndPosters" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("SignsAndPosters")}>
                <Icon icon="solid-clipboard-user" fa />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Accessory")} description={_L("FindStuff.SubFilter.Accessory_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Accessory" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Accessory")}>
                <Icon icon="solid-tree-city" fa />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Vehicle")} description={_L("FindStuff.SubFilter.Vehicle_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Vehicle" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Vehicle")}>
                <Icon icon="Media/Game/Icons/Traffic.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.PropMisc")} description={_L("FindStuff.SubFilter.PropMisc_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "PropMisc" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("PropMisc")}>
                <Icon icon="solid-ellipsis" fa/>
            </Button>
        </div>;
    }

    else if (model.Filter === "Network") {
        return <div className={containerClassName}>
            {subOptionsHeader}
            <Button title={_L("FindStuff.SubFilter.SmallRoad")} description={_L("FindStuff.SubFilter.SmallRoad_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "SmallRoad" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("SmallRoad")}>
                <Icon icon="Media/Game/Icons/SmallRoad.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.MediumRoad")} description={_L("FindStuff.SubFilter.MediumRoad_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "MediumRoad" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("MediumRoad")}>
                <Icon icon="Media/Game/Icons/MediumRoad.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.LargeRoad")} description={_L("FindStuff.SubFilter.LargeRoad_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "LargeRoad" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("LargeRoad")}>
                <Icon icon="Media/Game/Icons/LargeRoad.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Highway")} description={_L("FindStuff.SubFilter.Highway_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Highway" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Highway")}>
                <Icon icon="Media/Game/Icons/Highways.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Roundabout")} description={_L("FindStuff.SubFilter.Roundabout_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Roundabout" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Roundabout")}>
                <Icon icon="Media/Game/Icons/Roundabouts.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Pavement")} description={_L("FindStuff.SubFilter.Pavement_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Pavement" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Pavement")}>
                <Icon icon="Media/Game/Icons/Pathways.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.RoadTool")} description={_L("FindStuff.SubFilter.RoadTool_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "RoadTool" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("RoadTool")}>
                <Icon icon="Media/Game/Icons/RoadsServices.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.OtherNetwork")} description={_L("FindStuff.SubFilter.OtherNetwork_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "OtherNetwork" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("OtherNetwork")}>
                <Icon icon="solid-ellipsis" fa />
            </Button>
        </div>;
    }
    else if (model.Filter === "Misc") {
        return <div className={containerClassName}>
            {subOptionsHeader}
            <Button title={_L("FindStuff.SubFilter.Surface")} description={_L("FindStuff.SubFilter.Surface_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Surface" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Surface")}>
                <Icon icon="Media/Game/Icons/LotTool.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.Area")} description={_L("FindStuff.SubFilter.Area_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "Area" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Area")}>
                <Icon icon="Media/Game/Icons/LotTool.svg" />
            </Button>
            <Button title={_L("FindStuff.SubFilter.TransportStop")} description={_L("FindStuff.TransportStop.Area_desc")}
                toolTipFloat={isVertical ? "down" : "up"} className={"ml-1" + (model.SubFilter === "TransportStop" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("TransportStop")}>
                <Icon icon="Media/Game/Icons/Bus Stop.svg" />
            </Button>
        </div>;
    }

    return <></>;
};

export default SubFilters;