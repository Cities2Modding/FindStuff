import React from "react";
import debounce from "lodash.debounce";

const SearchField = ({ className, showLoading, textBoxClassName, model, updateModel, onUpdate, debounceDelay = 100, _L }) => {
    const react = window.$_gooee.react;
    const { TextBox, Button, Icon, Container, useDebouncedCallback } = window.$_gooee.framework;

    const isVertical = model.OperationMode === "HideFindStuffSideMenu";
    const [showTypeahead, setShowTypeahead] = react.useState(false);
    const [search, setSearch] = react.useState(model.Search ?? "");
    const searchRef = react.useRef(search);

    const debouncedUpdate = useDebouncedCallback(
        // function
        (value) => {
            model.Search = value;
            updateModel("Search", value);

            if (onUpdate)
                onUpdate();
        },
        // delay in ms
        debounceDelay
    );
    //const debouncedSearchUpdate = react.useRef(debounce(onUpdate, debounceDelay)).current;

    //react.useEffect(() => {
    //    return () => {
    //        debouncedSearchUpdate.cancel(); // Cleanup on unmount
    //    };
    //}, [debouncedSearchUpdate]);

    react.useEffect(() => {
        searchRef.current = search;
    }, [search]);

    react.useEffect(() => {
        if (model.Search !== search) {
            setSearch(model.Search);
        }
    }, [model.Search]);
    
    const clearSearch = () => {
        setSearch("");
        model.Search = "";
        updateModel("Search", "");
        if (onUpdate)
            onUpdate();
        setShowTypeahead(false);
    };

    const typeaheads = react.useMemo(() => {
        if ((search && search.length == 0) || !search)
            return model.RecentSearches;

        return model.RecentSearches.filter(s => s !== search && s.toLowerCase().includes(search.toLowerCase()));
    }, [model.RecentSearches, search]);

    const highlightSearchTerm = (text) => {
        // Check if there's a search term; if not, return the original text
        if (!search || search.trim().length === 0)
            return text;

        // Split the search term into individual words and escape regex special characters
        const words = search.trim().split(/\s+/).map(word =>
            word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
        );

        // Create a regex to match any of the words, case-insensitively
        const regex = new RegExp(`(${words.join('|')})`, 'gi');

        // Split the text by the regex, keeping matched parts for highlighting
        const splitText = text.split(regex);

        // Map through the split text to highlight matched parts
        return splitText.map((part, index) =>
            regex.test(part) ? (
                <span key={index} className="text-dark bg-warning">
                    <b>{part}</b>
                </span>
            ) : part
        );
    };

    const onSearchInputChanged = (val) => {
        //if (showLoading && searchRef.current) {
        //    searchRef.current.blur();
        //    return;
        //}
       
        setSearch(val);
        debouncedUpdate(val);

        if (typeaheads && typeaheads.length > 0 && !showTypeahead) {
            setShowTypeahead(true);
        }
    };

    const onItemClick = react.useCallback((text) => {
        model.Search = text;
        setSearch(model.Search);
        updateModel("Search", model.Search);
        if (onUpdate)
            onUpdate();
        setShowTypeahead(false);
        engine.trigger("audio.playSound", "select-item", 1);
    }, [model.Search]);

    const onTextBoxClick = () => {
        if (!showLoading && (!model.Search || model.Search.length == 0)) {
            setShowTypeahead(true);
        }
    };

    const onTextBoxBlur = () => {
        //setShowTypeahead(false);
    };

    const onTypeaheadHidden = () => {
        setShowTypeahead(false);
    };

    const dropDownMenu = typeaheads && typeaheads.length > 0 ? <div className="faux-dropdown-menu dropdown-menu-sm vw-7">
        {typeaheads.map((s, index) => (<div key={index} className="dropdown-item" onClick={() => onItemClick(s)}>{highlightSearchTerm(s)}</div>))}
    </div> : null;

    return <> <Container className={"d-inline h-x w-x " + className} onDropdownHidden={onTypeaheadHidden}
        dropdownMenu={dropDownMenu} showDropDown={showTypeahead} dropdownCloseOnClickOutside={true}>
        <TextBox size="sm" selectOnFocus="true" onClick={onTextBoxClick} onBlur={onTextBoxBlur} className={"bg-dark-trans-less-faded " + (isVertical ? "vw-10 " : "vw-7 ") + textBoxClassName} placeholder="Search..." text={search} onChange={onSearchInputChanged} />
        </Container>
        <Button className="ml-2"
            title={_L("FindStuff.ClearSearch")} description={_L("FindStuff.ClearSearch_desc")}
            toolTipFloat={isVertical ? "down" : "up"}
            circular icon style="trans-faded" disabled={search && search.length > 0 ? null : true} onClick={clearSearch}>
            <Icon icon="solid-eraser" fa />
        </Button>
    </>;
};

export default SearchField;