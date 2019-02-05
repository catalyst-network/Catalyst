First make sure you have vagrant && virtualbox(5.1) installed.

You must install Vagrant 2.2 from the url not apt.

	https://www.virtualbox.org/wiki/Downloads
	https://www.vagrantup.com/docs/installation/

then make sure you have the vagrant a and virtualbox guest additions installed.

	vagrant plugin install vagrant-vbguest


Make sure you have updated the Vagrantfile so your sync folder paths are correct.

then run:

	vagrant up --provision
