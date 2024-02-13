import React from "react";
import debounce from "lodash.debounce";

const SearchField = ({ className, model, updateModel, onUpdate, debounceDelay = 100, _L }) => {
    const react = window.$_gooee.react;
    const { TextBox, Button, Icon } = window.$_gooee.framework;

    const [search, setSearch] = react.useState(model.Search ?? "");
    const searchRef = react.useRef(search);

    react.useEffect(() => {
        searchRef.current = search;
    }, [search]);

    react.useEffect(() => {
        if (model.Search !== search) {
            setSearch(model.Search);
        }
    }, [model.Search]);

    const debouncedSearchUpdate = debounce(onUpdate, debounceDelay);

    const onSearchInputChanged = (val) => {
        model.Search = val;
        updateModel("Search", val);
        setSearch(val);
        debouncedSearchUpdate();
    };

    const clearSearch = () => {
        setSearch("");
        model.Search = "";
        updateModel("Search", "");
        debouncedSearchUpdate();
    };

    return <>
        <TextBox size="sm" className={"bg-dark-trans-less-faded mr-2 " + className} placeholder="Search..." text={search} onChange={onSearchInputChanged} />
        <Button title={_L("FindStuff.ClearSearch")} description={_L("FindStuff.ClearSearch_desc")} circular icon style="trans-faded" disabled={search && search.length > 0 ? null : true} onClick={clearSearch}>
            <Icon icon="solid-eraser" fa />
        </Button>
    </>;
};

export default SearchField;