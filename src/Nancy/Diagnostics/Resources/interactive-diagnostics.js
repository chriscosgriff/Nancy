﻿Backbone.StaticDiagnosticsView = Backbone.LayoutManager.extend({
    fetch: function (path) {
        var done = this.async();

        $.get(path + ".html")
         .success(function (contents) { done(contents); })
         .error(function () { done(null); });
    },

    render: function (template, context) {
        // If we have a template then render it with handlebars
        if (template) {
            return Handlebars.compile(template)(context);
        }

        // Otherwise fallback to using the json renderer
        return _.jsonreport(context);
    }
});

$(function () {
    var app = diagnostics.app;

    var Provider = diagnostics.module("provider");

    var Router = Backbone.Router.extend({
        routes: {
            "": "index"
        },

        fetchProviders: function () {
            var cache;

            return function (done) {
                if (cache) {
                    return done(cache);
                }

                var providers = new Provider.Collection();

                providers.fetch().success(function () {
                    cache = providers;
                    done(cache);
                });
            };
        } (),

        index: function () {
            this.fetchProviders(function (providers) {
                var list = new Provider.Views.List({ model: providers });
                list.render();
            });
        }
    });

    // Start router and trigger first route
    app.router = new Router();
    Backbone.history.start();
});