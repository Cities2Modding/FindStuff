import React from "react";

const SubFilters = ({ model, update, onDoUpdate, _L }) => {
    const react = window.$_gooee.react;
    const { Icon, Button } = window.$_gooee.framework;    

    const updateBackend = (val) => {
        model.SubFilter = val;
        update("SubFilter", val);

        if (onDoUpdate)
            onDoUpdate(model);
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
            <Button className={(!model.SubFilter || model.SubFilter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("None")}>
                <Icon icon="solid-asterisk" fa />
            </Button>
        </>
 /*   ), [model.Filter, model.SubFilter, update]);*/

    if (model.Filter === "Zones") {
        return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
            {subOptionsHeader}
            <Button className={"ml-1" + (model.SubFilter === "ZoneResidential" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneResidential")}>
                <Icon icon="Media/Game/Icons/ZoneResidential.svg" />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "ZoneCommercial" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneCommercial")}>
                <Icon icon="Media/Game/Icons/ZoneCommercial.svg" />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "ZoneOffice" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneOffice")}>
                <Icon icon="Media/Game/Icons/ZoneOffice.svg" />
            </Button>
            <Button className={"ml-1 mr-1" + (model.SubFilter === "ZoneIndustrial" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ZoneIndustrial")}>
                <Icon icon="Media/Game/Icons/ZoneIndustrial.svg" />
            </Button>
        </div>;
    }
    else if (model.Filter === "Buildings") {
        return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
            {subOptionsHeader}
            <Button className={"ml-1" + (model.SubFilter === "ServiceBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("ServiceBuilding")}>
                <Icon icon="Media/Game/Icons/Services.svg" />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "SignatureBuilding" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("SignatureBuilding")}>
                <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
            </Button>
        </div>;
    }
    else if (model.Filter === "Foliage") {
        return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
            {subOptionsHeader}
            <Button className={"ml-1" + (model.SubFilter === "Tree" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Tree")}>
                <Icon icon="Media/Game/Icons/Forest.svg" />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "Plant" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Plant")}>
                <Icon icon="Media/Game/Icons/Forest.svg" />
            </Button>
        </div>;
    }
    else if (model.Filter === "Props") {
        return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
            {subOptionsHeader}
            <Button className={"ml-1" + (model.SubFilter === "Billboards" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Billboards")}>
                <Icon icon="solid-bullhorn" fa />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "Fences" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Fences")}>
                <Icon icon="solid-xmarks-lines" fa />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "SignsAndPosters" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("SignsAndPosters")}>
                <Icon icon="solid-sign-hanging" fa />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "Vehicle" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Vehicle")}>
                <Icon icon="Media/Game/Icons/Traffic.svg" />
            </Button>
            <Button className={"ml-1" + (model.SubFilter === "PropMisc" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("PropMisc")}>
                <Icon icon="solid-ellipsis" fa/>
            </Button>
        </div>;
    }
    else if (model.Filter === "Misc") {
        return <div className="d-flex flex-row flex-wrap justify-content-end mr-6 flex-1">
            {subOptionsHeader}
            <Button className={"ml-1" + (model.SubFilter === "Surface" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateBackend("Surface")}>
                <Icon icon="Media/Game/Icons/LotTool.svg" />
            </Button>
        </div>;
    }

    return <></>;
};

export default SubFilters;