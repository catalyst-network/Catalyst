# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  
  config.ssh.shell="bash"

  config.vm.define "node1" do |node1|
  	node1.vm.box = "ubuntu/xenial64"
  end
  config.vm.define "node2" do |node2|
  	node2.vm.box = "ubuntu/xenial64"
  end  
  config.vm.define "node3" do |node3|
  	node3.vm.box = "ubuntu/xenial64"
  end  
  
  config.vm.network "public_network",
  use_dhcp_assigned_default_route: true
   
  config.vm.synced_folder "/srv/Dev/ADL", "/srv"

  config.vm.provider "virtualbox" do |vb|
    vb.gui = false
    vb.memory = "2512"
  end

  config.vm.provision "shell", inline: <<-SHELL
    add-apt-repository universe
	apt-get install apt-transport-https
	wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
	sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
	wget -q https://packages.microsoft.com/config/ubuntu/16.04/prod.list
	sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
	sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
	sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list
	apt update
	apt upgrade
  	apt install -f -y openssl dotnet-runtime-deps-2.1 htop lsof git dotnet-sdk-2.1.105
  SHELL
  
end
