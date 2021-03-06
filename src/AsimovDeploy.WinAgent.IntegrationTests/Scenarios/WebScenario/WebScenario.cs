﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AsimovDeploy.WinAgent.Framework.Configuration;
using AsimovDeploy.WinAgent.Framework.Models;
using AsimovDeploy.WinAgent.Web.Commands;
using AsimovDeploy.WinAgent.Web.Contracts;
using NUnit.Framework;
using Shouldly;

namespace AsimovDeploy.WinAgent.IntegrationTests.Scenarios.WebScenario
{
    [TestFixture]
    public class WebScenario : WinAgentSystemTest
    {
        private const string ServiceName = "Asimov.Web.Example";

        public override void Given()
        {
            GivenFoldersForScenario();
            GivenRunningAgent();
            EnsureServiceIsNotInstalled();
        }

        [TearDown]
        public void AfterEach()
        {
            EnsureServiceIsNotInstalled();
        }

        private void EnsureServiceIsNotInstalled()
        {
            var units = Agent.Get<List<DeployUnitInfoDTO>>("/units/list");
            if (units[0].status == "NotFound")
            {
                return;
            }
            Agent.Post("/action",NodeFront.ApiKey, new UnitActionCommand()
            {
                actionName = "Uninstall",
                unitName = ServiceName
            });
            WaitForStatus("NotFound");
            while (Process.GetProcesses().Any(x => x.ProcessName == "Asimov.Roundhouse.Example"))
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }
        }

        [Test]
        public void can_get_deploy_units()
        {
            var units = Agent.Get<List<DeployUnitInfoDTO>>("/units/list");
            units.Count.ShouldBe(2);
            units[0].name.ShouldBe(ServiceName);
            units[0].status.ShouldBe("NotFound");
            units[0].type.ShouldBe(DeployUnitTypes.WebSite);
        }

        [Test]
        public void installs_service_on_deploy()
        {
            InstallService();

            var units = Agent.Get<List<DeployUnitInfoDTO>>("/units/list");
            units.Count.ShouldBe(2);
            units[0].name.ShouldBe(ServiceName);
            units[0].status.ShouldBe("Running");
            units[0].type.ShouldBe(DeployUnitTypes.WebSite);
        }

        private void InstallService()
        {
            var versions = Agent.Get<List<DeployUnitVersionDTO>>($"/versions/{ServiceName}");
            versions.Count.ShouldBe(1);

            Agent.Post("/deploy/deploy", NodeFront.ApiKey, new DeployCommand
            {
                unitName = ServiceName,
                versionId = versions[0].id,
                parameters = new Dictionary<string, object>() {
                {
                    "Port", "8145"
                } }
            });

            WaitForStatus("Running");
        }

        [Test]
        public void when_NotFound_gets_install_parameters()
        {
            var parameters = Agent.Get<List<TextActionParameter>>($"/units/deploy-parameters/{ServiceName}");

            parameters.Count.ShouldBe(1);
            parameters[0].Name.ShouldBe("Port");
            parameters[0].Default.ShouldBe("8123");
        }

        [Test]
        public void when_Installed_gets_deploy_parameters()
        {
            InstallService();

            var parameters = Agent.Get<List<TextActionParameter>>($"/units/deploy-parameters/{ServiceName}");

            parameters.Count.ShouldBe(1);
            parameters[0].Name.ShouldBe("NotUsed");
        }

        private void WaitForStatus(string expectedStatus)
        {
            var start = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(10);
            var duration = DateTime.Now - start;
            var status = "";

            do
            {
                duration = DateTime.Now - start;
                var units = Agent.Get<List<DeployUnitInfoDTO>>("/units/list");
                units.Count.ShouldBe(2);
                status = units[0].status;
                if (status == expectedStatus)
                {
                    return;
                }
            } while (duration < timeout);
            
            Assert.Fail($"Failed to go to correct status, was {status} expected {expectedStatus}");

        }
    }
}