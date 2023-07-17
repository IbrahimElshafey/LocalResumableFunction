
function searchLogs() {
    var serviceId = document.getElementById("selectedService").value;
    var statusCode = document.getElementById("selectedStatusCode").value;
    setMainPageView(`/RF/Home/_LatestLogs?serviceId=${serviceId}&statusCode=${statusCode}`);
}

function resetLogs() {
    setMainPageView(`/RF/Home/_LatestLogs`);
}

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
    setMainPageView(url, index)
    window.location.hash = "#view=" + index;
}

function searchFunctions() {
    var serviceId = document.getElementById("selectedService").value;
    var functionSearchTerm = document.getElementById("functionSearchTerm").value;
    setMainPageView(`/RF/Home/_ResumableFunctionsList?serviceId=${serviceId}&functionName=${functionSearchTerm}`);
}

function resetFunctionsView() {
    setMainPageView(`/RF/Home/_ResumableFunctionsList`);
}

function searchMethodGroups() {
    var serviceId = document.getElementById("selectedService").value;
    var searchTerm = document.getElementById("searchTerm").value;
    setMainPageView(`/RF/Home/_MethodGroups?serviceId=${serviceId}&searchTerm=${searchTerm}`);
}

function resetMethodGroups() {
    setMainPageView(`/RF/Home/_MethodGroups`);
}

function searchPushedCalls() {
    var serviceId = document.getElementById("selectedService").value;
    var searchTerm = document.getElementById("searchTerm").value;
    setMainPageView(`/RF/Home/_PushedCalls?serviceId=${serviceId}&searchTerm=${searchTerm}`);
}

function resetPushedCalls() {
    setMainPageView(`/RF/Home/_PushedCalls`);
}