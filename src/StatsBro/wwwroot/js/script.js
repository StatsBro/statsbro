!function () {
	"use strict";
	var cs = document.currentScript;
	var v = 1, u = document.location.href, r = document.referrer, w = window.innerWidth, h = window.innerHeight, tp = navigator.maxTouchPoints, l = navigator.language, ua = navigator.userAgent;

	function t(e = 'pageview') {
		var req = new XMLHttpRequest;	
		var o = { url: u, r: r, ww: w, wh: h, tp: tp, l: l, ua: ua, e: e, v: v };
		var url = cs.getAttribute("data-api") || "https://statsbro.io/api/s";
		req.open("POST", url);
		req.send(JSON.stringify(o));
    }

	window.statsbro = t;
	
	t();

	window.addEventListener('popstate', t);
	window.addEventListener('hashchange', t);
}();