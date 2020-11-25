function OnInit(func) {
    if (document.readyState !== 'complete')
        $(document).ready(function () {
            func();
        });
    else
        func();
}