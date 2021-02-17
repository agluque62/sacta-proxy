angular.module("sacta_proxy")
    .controller("SprxConfigCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        ctrl.config = undefined;
        ctrl.selecteddep = "0";
        ctrl.selecteddep_mirror = "0";
        ctrl.dep = {
            cfg: null,
            isdep: false,
        };
        ctrl.topage = (page) => {
            ConsolidateFromPage(ctrl.pagina);
            DataLoadOfPage(page);
            ctrl.pagina = page;
        }
        ctrl.update = () => {
            alertify.success("Updating config..")
            ConsolidateFromPage(ctrl.pagina);
        };
        ctrl.reset = () => {
            /** Obtiene de nuevo la configuracion del servidor */
            load_config(() => {
                /** reinicia los estados del form de configuracion */
                $scope.cfgform.$setPristine();
                DataLoadOfPage(ctrl.pagina);
            });
        };
        ctrl.changedep = () => {
            ConsolidateFromPage(ctrl.pagina);
            ctrl.selecteddep_mirror = ctrl.selecteddep;
            DataLoadOfPage(ctrl.pagina);
        };

        function ConsolidateFromPage(page) {
            if (page == 1) {
                ctrl.config.Psi = ctrl.dep.cfg;
            }
            else if (page == 2) {
                ctrl.config.Dependencies[ctrl.selecteddep_mirror] = ctrl.dep.cfg;
            }
        }

        function DataLoadOfPage(page) {
            if (page == 1) {
                if (ctrl.config) {
                    ctrl.dep.cfg = ctrl.config.Psi;
                    ctrl.dep.isdep = false;
                }
            }
            else if (page == 2) {
                if (ctrl.config) {
                    ctrl.dep.cfg = ctrl.config.Dependencies[ctrl.selecteddep_mirror];
                    ctrl.dep.isdep = true;
                }
            }
        }

        function load_config(sync) {
            $serv.config((config) => {
                if (config.res == "ok") {
                    ctrl.config = config.cfg;
                    if (sync) sync();
                }
                else {
                    // todo
                }
            });
        }

        /** */
        $scope.$on('$viewContentLoaded', function () {
            load_config();
        });
        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {

        });
    });
