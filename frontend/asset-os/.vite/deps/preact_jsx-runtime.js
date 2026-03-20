import { k, l } from "./chunk-PGNU5USP.js";

// node_modules/.deno/preact@10.28.3/node_modules/preact/jsx-runtime/dist/jsxRuntime.module.js
var t = /["&<]/;
function n(r) {
  if (0 === r.length || false === t.test(r)) return r;
  for (var e2 = 0, n2 = 0, o2 = "", f2 = ""; n2 < r.length; n2++) {
    switch (r.charCodeAt(n2)) {
      case 34:
        f2 = "&quot;";
        break;
      case 38:
        f2 = "&amp;";
        break;
      case 60:
        f2 = "&lt;";
        break;
      default:
        continue;
    }
    n2 !== e2 && (o2 += r.slice(e2, n2)), o2 += f2, e2 = n2 + 1;
  }
  return n2 !== e2 && (o2 += r.slice(e2, n2)), o2;
}
var o = /acit|ex(?:s|g|n|p|$)|rph|grid|ows|mnc|ntw|ine[ch]|zoo|^ord|itera/i;
var f = 0;
var i = Array.isArray;
function u(e2, t2, n2, o2, i2, u2) {
  t2 || (t2 = {});
  var a2, c2, p2 = t2;
  if ("ref" in p2) {
    for (c2 in p2 = {}, t2) "ref" == c2 ? a2 = t2[c2] : p2[c2] = t2[c2];
  }
  var l3 = {
    type: e2,
    props: p2,
    key: n2,
    ref: a2,
    __k: null,
    __: null,
    __b: 0,
    __e: null,
    __c: null,
    constructor: void 0,
    __v: --f,
    __i: -1,
    __u: 0,
    __source: i2,
    __self: u2,
  };
  if ("function" == typeof e2 && (a2 = e2.defaultProps)) {
    for (c2 in a2) void 0 === p2[c2] && (p2[c2] = a2[c2]);
  }
  return l.vnode && l.vnode(l3), l3;
}
function a(r) {
  var t2 = u(e, { tpl: r, exprs: [].slice.call(arguments, 1) });
  return t2.key = t2.__v, t2;
}
var c = {};
var p = /[A-Z]/g;
function l2(e2, t2) {
  if (l.attr) {
    var f2 = l.attr(e2, t2);
    if ("string" == typeof f2) return f2;
  }
  if (
    t2 = (function (r) {
      return null !== r && "object" == typeof r &&
          "function" == typeof r.valueOf
        ? r.valueOf()
        : r;
    })(t2), "ref" === e2 || "key" === e2
  ) return "";
  if ("style" === e2 && "object" == typeof t2) {
    var i2 = "";
    for (var u2 in t2) {
      var a2 = t2[u2];
      if (null != a2 && "" !== a2) {
        var l3 = "-" == u2[0]
            ? u2
            : c[u2] || (c[u2] = u2.replace(p, "-$&").toLowerCase()),
          s2 = ";";
        "number" != typeof a2 || l3.startsWith("--") || o.test(l3) ||
        (s2 = "px;"), i2 = i2 + l3 + ":" + a2 + s2;
      }
    }
    return e2 + '="' + n(i2) + '"';
  }
  return null == t2 || false === t2 || "function" == typeof t2 ||
      "object" == typeof t2
    ? ""
    : true === t2
    ? e2
    : e2 + '="' + n("" + t2) + '"';
}
function s(r) {
  if (null == r || "boolean" == typeof r || "function" == typeof r) return null;
  if ("object" == typeof r) {
    if (void 0 === r.constructor) return r;
    if (i(r)) {
      for (var e2 = 0; e2 < r.length; e2++) r[e2] = s(r[e2]);
      return r;
    }
  }
  return n("" + r);
}
export {
  a as jsxTemplate,
  k as Fragment,
  l2 as jsxAttr,
  s as jsxEscape,
  u as jsx,
  u as jsxDEV,
  u as jsxs,
};
