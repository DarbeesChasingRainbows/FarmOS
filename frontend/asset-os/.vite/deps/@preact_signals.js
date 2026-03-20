import { A, d, T, y } from "./chunk-OI4XJ75V.js";
import { k, l, t, x } from "./chunk-PGNU5USP.js";

// node_modules/.deno/@preact+signals-core@1.13.0/node_modules/@preact/signals-core/dist/signals-core.module.js
var i = /* @__PURE__ */ Symbol.for("preact-signals");
function t2() {
  if (!(s > 1)) {
    var i2, t3 = false;
    while (void 0 !== h) {
      var n2 = h;
      h = void 0;
      v++;
      while (void 0 !== n2) {
        var r2 = n2.o;
        n2.o = void 0;
        n2.f &= -3;
        if (!(8 & n2.f) && a(n2)) {
          try {
            n2.c();
          } catch (n3) {
            if (!t3) {
              i2 = n3;
              t3 = true;
            }
          }
        }
        n2 = r2;
      }
    }
    v = 0;
    s--;
    if (t3) throw i2;
  } else s--;
}
function n(i2) {
  if (s > 0) return i2();
  s++;
  try {
    return i2();
  } finally {
    t2();
  }
}
var r = void 0;
function o(i2) {
  var t3 = r;
  r = void 0;
  try {
    return i2();
  } finally {
    r = t3;
  }
}
var f;
var h = void 0;
var s = 0;
var v = 0;
var u = 0;
function e(i2) {
  if (void 0 !== r) {
    var t3 = i2.n;
    if (void 0 === t3 || t3.t !== r) {
      t3 = {
        i: 0,
        S: i2,
        p: r.s,
        n: void 0,
        t: r,
        e: void 0,
        x: void 0,
        r: t3,
      };
      if (void 0 !== r.s) r.s.n = t3;
      r.s = t3;
      i2.n = t3;
      if (32 & r.f) i2.S(t3);
      return t3;
    } else if (-1 === t3.i) {
      t3.i = 0;
      if (void 0 !== t3.n) {
        t3.n.p = t3.p;
        if (void 0 !== t3.p) t3.p.n = t3.n;
        t3.p = r.s;
        t3.n = void 0;
        r.s.n = t3;
        r.s = t3;
      }
      return t3;
    }
  }
}
function d2(i2, t3) {
  this.v = i2;
  this.i = 0;
  this.n = void 0;
  this.t = void 0;
  this.W = null == t3 ? void 0 : t3.watched;
  this.Z = null == t3 ? void 0 : t3.unwatched;
  this.name = null == t3 ? void 0 : t3.name;
}
d2.prototype.brand = i;
d2.prototype.h = function () {
  return true;
};
d2.prototype.S = function (i2) {
  var t3 = this, n2 = this.t;
  if (n2 !== i2 && void 0 === i2.e) {
    i2.x = n2;
    this.t = i2;
    if (void 0 !== n2) n2.e = i2;
    else {o(function () {
        var i3;
        null == (i3 = t3.W) || i3.call(t3);
      });}
  }
};
d2.prototype.U = function (i2) {
  var t3 = this;
  if (void 0 !== this.t) {
    var n2 = i2.e, r2 = i2.x;
    if (void 0 !== n2) {
      n2.x = r2;
      i2.e = void 0;
    }
    if (void 0 !== r2) {
      r2.e = n2;
      i2.x = void 0;
    }
    if (i2 === this.t) {
      this.t = r2;
      if (void 0 === r2) {
        o(function () {
          var i3;
          null == (i3 = t3.Z) || i3.call(t3);
        });
      }
    }
  }
};
d2.prototype.subscribe = function (i2) {
  var t3 = this;
  return m(function () {
    var n2 = t3.value, o2 = r;
    r = void 0;
    try {
      i2(n2);
    } finally {
      r = o2;
    }
  }, { name: "sub" });
};
d2.prototype.valueOf = function () {
  return this.value;
};
d2.prototype.toString = function () {
  return this.value + "";
};
d2.prototype.toJSON = function () {
  return this.value;
};
d2.prototype.peek = function () {
  var i2 = r;
  r = void 0;
  try {
    return this.value;
  } finally {
    r = i2;
  }
};
Object.defineProperty(d2.prototype, "value", {
  get: function () {
    var i2 = e(this);
    if (void 0 !== i2) i2.i = this.i;
    return this.v;
  },
  set: function (i2) {
    if (i2 !== this.v) {
      if (v > 100) throw new Error("Cycle detected");
      this.v = i2;
      this.i++;
      u++;
      s++;
      try {
        for (var n2 = this.t; void 0 !== n2; n2 = n2.x) n2.t.N();
      } finally {
        t2();
      }
    }
  },
});
function c(i2, t3) {
  return new d2(i2, t3);
}
function a(i2) {
  for (var t3 = i2.s; void 0 !== t3; t3 = t3.n) {
    if (t3.S.i !== t3.i || !t3.S.h() || t3.S.i !== t3.i) return true;
  }
  return false;
}
function l2(i2) {
  for (var t3 = i2.s; void 0 !== t3; t3 = t3.n) {
    var n2 = t3.S.n;
    if (void 0 !== n2) t3.r = n2;
    t3.S.n = t3;
    t3.i = -1;
    if (void 0 === t3.n) {
      i2.s = t3;
      break;
    }
  }
}
function y2(i2) {
  var t3 = i2.s, n2 = void 0;
  while (void 0 !== t3) {
    var r2 = t3.p;
    if (-1 === t3.i) {
      t3.S.U(t3);
      if (void 0 !== r2) r2.n = t3.n;
      if (void 0 !== t3.n) t3.n.p = r2;
    } else n2 = t3;
    t3.S.n = t3.r;
    if (void 0 !== t3.r) t3.r = void 0;
    t3 = r2;
  }
  i2.s = n2;
}
function w(i2, t3) {
  d2.call(this, void 0);
  this.x = i2;
  this.s = void 0;
  this.g = u - 1;
  this.f = 4;
  this.W = null == t3 ? void 0 : t3.watched;
  this.Z = null == t3 ? void 0 : t3.unwatched;
  this.name = null == t3 ? void 0 : t3.name;
}
w.prototype = new d2();
w.prototype.h = function () {
  this.f &= -3;
  if (1 & this.f) return false;
  if (32 == (36 & this.f)) return true;
  this.f &= -5;
  if (this.g === u) return true;
  this.g = u;
  this.f |= 1;
  if (this.i > 0 && !a(this)) {
    this.f &= -2;
    return true;
  }
  var i2 = r;
  try {
    l2(this);
    r = this;
    var t3 = this.x();
    if (16 & this.f || this.v !== t3 || 0 === this.i) {
      this.v = t3;
      this.f &= -17;
      this.i++;
    }
  } catch (i3) {
    this.v = i3;
    this.f |= 16;
    this.i++;
  }
  r = i2;
  y2(this);
  this.f &= -2;
  return true;
};
w.prototype.S = function (i2) {
  if (void 0 === this.t) {
    this.f |= 36;
    for (var t3 = this.s; void 0 !== t3; t3 = t3.n) t3.S.S(t3);
  }
  d2.prototype.S.call(this, i2);
};
w.prototype.U = function (i2) {
  if (void 0 !== this.t) {
    d2.prototype.U.call(this, i2);
    if (void 0 === this.t) {
      this.f &= -33;
      for (var t3 = this.s; void 0 !== t3; t3 = t3.n) t3.S.U(t3);
    }
  }
};
w.prototype.N = function () {
  if (!(2 & this.f)) {
    this.f |= 6;
    for (var i2 = this.t; void 0 !== i2; i2 = i2.x) i2.t.N();
  }
};
Object.defineProperty(w.prototype, "value", {
  get: function () {
    if (1 & this.f) throw new Error("Cycle detected");
    var i2 = e(this);
    this.h();
    if (void 0 !== i2) i2.i = this.i;
    if (16 & this.f) throw this.v;
    return this.v;
  },
});
function b(i2, t3) {
  return new w(i2, t3);
}
function _(i2) {
  var n2 = i2.u;
  i2.u = void 0;
  if ("function" == typeof n2) {
    s++;
    var o2 = r;
    r = void 0;
    try {
      n2();
    } catch (t3) {
      i2.f &= -2;
      i2.f |= 8;
      p(i2);
      throw t3;
    } finally {
      r = o2;
      t2();
    }
  }
}
function p(i2) {
  for (var t3 = i2.s; void 0 !== t3; t3 = t3.n) t3.S.U(t3);
  i2.x = void 0;
  i2.s = void 0;
  _(i2);
}
function g(i2) {
  if (r !== this) throw new Error("Out-of-order effect");
  y2(this);
  r = i2;
  this.f &= -2;
  if (8 & this.f) p(this);
  t2();
}
function S(i2, t3) {
  this.x = i2;
  this.u = void 0;
  this.s = void 0;
  this.o = void 0;
  this.f = 32;
  this.name = null == t3 ? void 0 : t3.name;
  if (f) f.push(this);
}
S.prototype.c = function () {
  var i2 = this.S();
  try {
    if (8 & this.f) return;
    if (void 0 === this.x) return;
    var t3 = this.x();
    if ("function" == typeof t3) this.u = t3;
  } finally {
    i2();
  }
};
S.prototype.S = function () {
  if (1 & this.f) throw new Error("Cycle detected");
  this.f |= 1;
  this.f &= -9;
  _(this);
  l2(this);
  s++;
  var i2 = r;
  r = this;
  return g.bind(this, i2);
};
S.prototype.N = function () {
  if (!(2 & this.f)) {
    this.f |= 2;
    this.o = h;
    h = this;
  }
};
S.prototype.d = function () {
  this.f |= 8;
  if (!(1 & this.f)) p(this);
};
S.prototype.dispose = function () {
  this.d();
};
function m(i2, t3) {
  var n2 = new S(i2, t3);
  try {
    n2.c();
  } catch (i3) {
    n2.d();
    throw i3;
  }
  var r2 = n2.d.bind(n2);
  r2[Symbol.dispose] = r2;
  return r2;
}

// node_modules/.deno/@preact+signals@2.4.0/node_modules/@preact/signals/dist/signals.module.js
var h2;
var l3;
var p2;
var w2 = [];
var m2 = [];
m(function () {
  h2 = this.N;
})();
function y3(i2, t3) {
  l[i2] = t3.bind(
    null,
    l[i2] || function () {
    },
  );
}
function _2(i2) {
  if (p2) p2();
  p2 = i2 && i2.S();
}
function g2(i2) {
  var n2 = this, r2 = i2.data, f2 = useSignal(r2);
  f2.value = r2;
  var e2 = T(function () {
      var i3 = n2, r3 = n2.__v;
      while (r3 = r3.__) {
        if (r3.__c) {
          r3.__c.__$f |= 4;
          break;
        }
      }
      var o2 = b(function () {
          var i4 = f2.value.value;
          return 0 === i4 ? 0 : true === i4 ? "" : i4 || "";
        }),
        e3 = b(function () {
          return !Array.isArray(o2.value) && !t(o2.value);
        }),
        u3 = m(function () {
          this.N = F;
          if (e3.value) {
            var n3 = o2.value;
            if (i3.__v && i3.__v.__e && 3 === i3.__v.__e.nodeType) {
              i3.__v.__e.data = n3;
            }
          }
        }),
        c3 = n2.__$u.d;
      n2.__$u.d = function () {
        u3();
        c3.call(this);
      };
      return [e3, o2];
    }, []),
    u2 = e2[0],
    c2 = e2[1];
  return u2.value ? c2.peek() : c2.value;
}
g2.displayName = "ReactiveTextNode";
Object.defineProperties(d2.prototype, {
  constructor: { configurable: true, value: void 0 },
  type: { configurable: true, value: g2 },
  props: {
    configurable: true,
    get: function () {
      return { data: this };
    },
  },
  __b: { configurable: true, value: 1 },
});
y3("__b", function (i2, n2) {
  if (
    "function" == typeof n2.type && "undefined" != typeof window &&
    window.__PREACT_SIGNALS_DEVTOOLS__
  ) window.__PREACT_SIGNALS_DEVTOOLS__.exitComponent();
  if ("string" == typeof n2.type) {
    var t3, r2 = n2.props;
    for (var o2 in r2) {
      if ("children" !== o2) {
        var f2 = r2[o2];
        if (f2 instanceof d2) {
          if (!t3) n2.__np = t3 = {};
          t3[o2] = f2;
          r2[o2] = f2.peek();
        }
      }
    }
  }
  i2(n2);
});
y3("__r", function (i2, n2) {
  if (
    "function" == typeof n2.type && "undefined" != typeof window &&
    window.__PREACT_SIGNALS_DEVTOOLS__
  ) window.__PREACT_SIGNALS_DEVTOOLS__.enterComponent(n2);
  if (n2.type !== k) {
    _2();
    var t3, o2 = n2.__c;
    if (o2) {
      o2.__$f &= -2;
      if (void 0 === (t3 = o2.__$u)) {
        o2.__$u = t3 = (function (i3) {
          var n3;
          m(function () {
            n3 = this;
          });
          n3.c = function () {
            o2.__$f |= 1;
            o2.setState({});
          };
          return n3;
        })();
      }
    }
    l3 = o2;
    _2(t3);
  }
  i2(n2);
});
y3("__e", function (i2, n2, t3, r2) {
  if ("undefined" != typeof window && window.__PREACT_SIGNALS_DEVTOOLS__) {
    window.__PREACT_SIGNALS_DEVTOOLS__.exitComponent();
  }
  _2();
  l3 = void 0;
  i2(n2, t3, r2);
});
y3("diffed", function (i2, n2) {
  if (
    "function" == typeof n2.type && "undefined" != typeof window &&
    window.__PREACT_SIGNALS_DEVTOOLS__
  ) window.__PREACT_SIGNALS_DEVTOOLS__.exitComponent();
  _2();
  l3 = void 0;
  var t3;
  if ("string" == typeof n2.type && (t3 = n2.__e)) {
    var r2 = n2.__np, o2 = n2.props;
    if (r2) {
      var f2 = t3.U;
      if (f2) {
        for (var e2 in f2) {
          var u2 = f2[e2];
          if (void 0 !== u2 && !(e2 in r2)) {
            u2.d();
            f2[e2] = void 0;
          }
        }
      } else {
        f2 = {};
        t3.U = f2;
      }
      for (var a2 in r2) {
        var c2 = f2[a2], v2 = r2[a2];
        if (void 0 === c2) {
          c2 = b2(t3, a2, v2, o2);
          f2[a2] = c2;
        } else c2.o(v2, o2);
      }
    }
  }
  i2(n2);
});
function b2(i2, n2, t3, r2) {
  var o2 = n2 in i2 && void 0 === i2.ownerSVGElement, f2 = c(t3);
  return {
    o: function (i3, n3) {
      f2.value = i3;
      r2 = n3;
    },
    d: m(function () {
      this.N = F;
      var t4 = f2.value.value;
      if (r2[n2] !== t4) {
        r2[n2] = t4;
        if (o2) i2[n2] = t4;
        else if (null != t4 && (false !== t4 || "-" === n2[4])) {
          i2.setAttribute(n2, t4);
        } else i2.removeAttribute(n2);
      }
    }),
  };
}
y3("unmount", function (i2, n2) {
  if ("string" == typeof n2.type) {
    var t3 = n2.__e;
    if (t3) {
      var r2 = t3.U;
      if (r2) {
        t3.U = void 0;
        for (var o2 in r2) {
          var f2 = r2[o2];
          if (f2) f2.d();
        }
      }
    }
  } else {
    var e2 = n2.__c;
    if (e2) {
      var u2 = e2.__$u;
      if (u2) {
        e2.__$u = void 0;
        u2.d();
      }
    }
  }
  i2(n2);
});
y3("__h", function (i2, n2, t3, r2) {
  if (r2 < 3 || 9 === r2) n2.__$f |= 2;
  i2(n2, t3, r2);
});
x.prototype.shouldComponentUpdate = function (i2, n2) {
  var t3 = this.__$u, r2 = t3 && void 0 !== t3.s;
  for (var o2 in n2) return true;
  if (this.__f || "boolean" == typeof this.u && true === this.u) {
    var f2 = 2 & this.__$f;
    if (!(r2 || f2 || 4 & this.__$f)) return true;
    if (1 & this.__$f) return true;
  } else {
    if (!(r2 || 4 & this.__$f)) return true;
    if (3 & this.__$f) return true;
  }
  for (var e2 in i2) {
    if ("__source" !== e2 && i2[e2] !== this.props[e2]) return true;
  }
  for (var u2 in this.props) if (!(u2 in i2)) return true;
  return false;
};
function useSignal(i2, n2) {
  return d(function () {
    return c(i2, n2);
  })[0];
}
function useComputed(i2, n2) {
  var t3 = A(i2);
  t3.current = i2;
  l3.__$f |= 4;
  return T(function () {
    return b(function () {
      return t3.current();
    }, n2);
  }, []);
}
var k2 = "undefined" == typeof requestAnimationFrame
  ? setTimeout
  : function (i2) {
    var n2 = function () {
        clearTimeout(t3);
        cancelAnimationFrame(r2);
        i2();
      },
      t3 = setTimeout(n2, 35),
      r2 = requestAnimationFrame(n2);
  };
var q = function (i2) {
  queueMicrotask(function () {
    queueMicrotask(i2);
  });
};
function A2() {
  n(function () {
    var i2;
    while (i2 = w2.shift()) h2.call(i2);
  });
}
function T2() {
  if (1 === w2.push(this)) (l.requestAnimationFrame || k2)(A2);
}
function x2() {
  n(function () {
    var i2;
    while (i2 = m2.shift()) h2.call(i2);
  });
}
function F() {
  if (1 === m2.push(this)) (l.requestAnimationFrame || q)(x2);
}
function useSignalEffect(i2, n2) {
  var t3 = A(i2);
  t3.current = i2;
  y(function () {
    return m(function () {
      this.N = T2;
      return t3.current();
    }, n2);
  }, []);
}
export {
  b as computed,
  c as signal,
  d2 as Signal,
  m as effect,
  n as batch,
  o as untracked,
  useComputed,
  useSignal,
  useSignalEffect,
};
//# sourceMappingURL=@preact_signals.js.map
