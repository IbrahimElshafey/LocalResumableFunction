function gotoViewPart(index, url) {
    console.log('gotoViewPart');
    if (index == undefined)
        index = '0';
    var i, tablinks;

    //de-activate all menu items
    tablinks = document.getElementsByClassName("tablink");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].className = tablinks[i].className.replace(" w3-indigo", "");
    }
    //open requested view
    //document.getElementById('partial-view-' +index).style.display = "block";
    //activate requestd menu item
    var currentButton = document.getElementById('tab-link-' + index);
    currentButton.className += " w3-indigo";
    setMainPageView(url, title(index))
    window.location.hash = "#view=" + index;
}

function searchFunctions() {
    var serviceId = document.getElementById("selectedService").value;
    var functionSearchTerm = document.getElementById("functionSearchTerm").value;
    setMainPageView(
        `/RF/Home/_ResumableFunctionsList?serviceId=${serviceId}&functionName=${functionSearchTerm}`, title(1));
}

function resetFunctionsView() {
    setMainPageView(`/RF/Home/_ResumableFunctionsList`, title(1));
}

function searchMethodGroups() {
    var serviceId = document.getElementById("selectedService").value;
    var searchTerm = document.getElementById("searchTerm").value;
    setMainPageView(`/RF/Home/_MethodGroups?serviceId=${serviceId}&searchTerm=${searchTerm}`,title(2));
}

function resetMethodGroups() {
    setMainPageView(`/RF/Home/_MethodGroups`, title(2));
}

function searchPushedCalls() {
    var serviceId = document.getElementById("selectedService").value;
    var searchTerm = document.getElementById("searchTerm").value;
    setMainPageView(`/RF/Home/_PushedCalls?serviceId=${serviceId}&searchTerm=${searchTerm}`, title(3));
}

function resetPushedCalls() {
    setMainPageView(`/RF/Home/_PushedCalls`, title(3));
}

function searchLogs() {
    var serviceId = document.getElementById("selectedService").value;
    var statusCode = document.getElementById("selectedStatusCode").value;
    setMainPageView(`/RF/Home/_LatestLogs?serviceId=${serviceId}&statusCode=${statusCode}`, title(4));
}

function resetLogs() {
    setMainPageView(`/RF/Home/_LatestLogs`, title(4));
}