/** */
angular.module("sacta_proxy")
    .controller("SprxGlobalCtrl", function ($scope, $interval, $location, $translate, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;

        ctrl.HashCode = 0;
        ctrl.timer = 0;
        ctrl.title = "";

        ctrl.user = "agl1";
        ctrl.date = (new Date()).toLocaleDateString();
        ctrl.hora = (new Date()).toLocaleTimeString();
        $location.path("/");

        /** Servicios de la pagina */
        ctrl.decodeHtml = function (html) {
            var txt = document.createElement("textarea");
            txt.innerHTML = html;
            return txt.value;
        };

        ctrl.logs = function () {
            var win = window.open('/logs', '_blank');
            win.focus();
        };

        ctrl.logout = function () {
            $serv.logout();
        }

    /** Funciones  */
        /**
         * */
        function get_status() {
            $serv.status((status) => {
                $lserv.GlobalStd(status);
            });
        }
        function get_inci() {
            $serv.inci_get((data) => {
                if (ctrl.HashCode != data.hash) {
                    ctrl.listainci = data.li;
                    ctrl.HashCode = data.hash;
                    inciPaginate();
                }
            });
        }
        /**
         * */
        function alive() {
            $serv.alive((data) => {
                ctrl.user=data.user;

            //        if (userLang != response.data.lang) {
            //            userLang = response.data.lang;
            //            if (userLang.indexOf("en") == 0)
            //                $translate.use('en_US');
            //            else if (userLang.indexOf("fr") == 0)
            //                $translate.use('fr_FR');
            //            else
            //                $translate.use('es_ES');
            //        }
            });
        }         
        /**
         * */
        function getTitle() {
            return "Nucleo Sacta Proxy";
        }
        /** 
         *  Funcion Periodica del controlador 
         * */
        var timer = $interval(function () {

            ctrl.date = moment().format('ll');
            ctrl.hora = moment().format('LTS');

            ctrl.timer++;

            if ((ctrl.timer % 5) == 0) {
                alive();
                get_status();
            }

            ctrl.title = getTitle();
        }, 1000);
        /** 
         */
        $scope.$on('$viewContentLoaded', function () {
            /** Alertify */
            alertify.defaults.transition = 'zoom';
            alertify.defaults.glossary = {
                title: $lserv.translate("Nucleo SACTA PROXY"),
                ok: $lserv.translate("Aceptar"),
                cancel: $lserv.translate("Cancelar")
            };
            get_status();
        });

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

    });

