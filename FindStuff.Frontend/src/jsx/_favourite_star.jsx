import React from "react";

const FavouriteStar = ({ model, onUpdateFavourite, prefab }) => {
    const react = window.$_gooee.react;
    const { Icon, Button } = window.$_gooee.framework;
    const isFavourite = react.useMemo(() => model.Favourites && model.Favourites.includes(prefab.Name), [model.Favourites, prefab.Name]);

    const onClick = (e) => {
        e.stopPropagation();

        if (!prefab || !onUpdateFavourite)
            return;

        onUpdateFavourite(prefab.Name);
    };

    const renderFavourite = () => {
        if (model.ViewMode === "Columns" || model.ViewMode === "Rows") {
            return <Button circular icon style="trans-faded" onClick={onClick} elementStyle={{ transform: 'scale(0.75)' }}>
                <Icon icon={(isFavourite ? "solid-star" : "star")} className={(isFavourite ? "bg-secondary" : "bg-secondary")} fa />
            </Button>;
        }
        return <div className="p-absolute p-top-0 p-left-0 w-100 h-100">
            <Button className="p-absolute p-right-0 p-top-0 mr-2 mb-2" circular icon style="trans-faded" onClick={onClick} elementStyle={{ transform: 'scale(0.75)', ...(model.ViewMode === "Detailed" ? { marginTop: '-2.5rem' } : null) }} >
                <Icon icon={(isFavourite ? "solid-star" : "star")} className={(isFavourite ? "bg-secondary" : "bg-secondary")} fa />
            </Button>
        </div>;
    };

    return renderFavourite();
};

export default FavouriteStar;