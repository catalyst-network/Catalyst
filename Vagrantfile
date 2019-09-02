$script = <<-SCRIPT

sudo mkdir /root/node
wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get install -y \
            apt-transport-https \
            ca-certificates \
            curl \
            gnupg-agent \
            software-properties-common

sudo apt-get update -y
sudo apt-get install dotnet-sdk-2.2 mongodb dnsutils lsof -y
SCRIPT

Vagrant.configure('2') do |config|

  config.vm.synced_folder ".", "/vagrant", disabled: true
  
config.vm.define "poa-1" do |config|

      config.vm.hostname = 'po1-1'

      config.vm.provider :digital_ocean do |provider, override|
        override.ssh.private_key_path = '/Users/nsh/.ssh/do'
        provider.ssh_key_name = 'catalyst-testnet'
        provider.monitoring = true
        override.vm.box = 'digital_ocean'
        override.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
        override.nfs.functional = false
        provider.token = 'SOME_SECURE_KEY_IN_HERE_PROBS_WANT_TO_USE_ENV_VARS'
        provider.image = 'ubuntu-18-04-x64'
        provider.region = 'tor1'
        provider.size = 's-1vcpu-1gb'
      end

      config.vm.provision "shell", inline: $script
  end

  config.vm.define "poa-2" do |config|

      config.vm.hostname = 'poa-2'

      config.vm.provider :digital_ocean do |provider, override|
        override.ssh.private_key_path = '/Users/nsh/.ssh/do'
        provider.ssh_key_name = 'catalyst-testnet'
        provider.monitoring = true
        override.vm.box = 'digital_ocean'
        override.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
        override.nfs.functional = false
        provider.token = 'SOME_SECURE_KEY_IN_HERE_PROBS_WANT_TO_USE_ENV_VARS'
        provider.image = 'ubuntu-18-04-x64'
        provider.region = 'sfo2'
        provider.size = 's-1vcpu-1gb'
      end

      config.vm.provision "shell", inline: $script
  end

  config.vm.define "poa-3" do |config|

      config.vm.hostname = 'poa-3'

      config.vm.provider :digital_ocean do |provider, override|
        override.ssh.private_key_path = '/Users/nsh/.ssh/do'
        provider.ssh_key_name = 'catalyst-testnet'
        provider.monitoring = true
        override.vm.box = 'digital_ocean'
        override.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
        override.nfs.functional = false
        provider.token = 'SOME_SECURE_KEY_IN_HERE_PROBS_WANT_TO_USE_ENV_VARS'
        provider.image = 'ubuntu-18-04-x64'
        provider.region = 'blr1'
        provider.size = 's-1vcpu-1gb'
      end

      config.vm.provision "shell", inline: $script
  end

  config.vm.define "poa-4" do |config|

      config.vm.hostname = 'poa-4'

      config.vm.provider :digital_ocean do |provider, override|
        override.ssh.private_key_path = '/Users/nsh/.ssh/do'
        provider.ssh_key_name = 'catalyst-testnet'
        provider.monitoring = true
        override.vm.box = 'digital_ocean'
        override.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
        override.nfs.functional = false
        provider.token = 'SOME_SECURE_KEY_IN_HERE_PROBS_WANT_TO_USE_ENV_VARS'
        provider.image = 'ubuntu-18-04-x64'
        provider.region = 'lon1'
        provider.size = 's-1vcpu-1gb'
      end

      config.vm.provision "shell", inline: $script
  end

  config.vm.define "poa-5" do |config|

      config.vm.hostname = 'poa-5'

      config.vm.provider :digital_ocean do |provider, override|
        override.ssh.private_key_path = '/Users/nsh/.ssh/do'
        provider.ssh_key_name = 'catalyst-testnet'
        provider.monitoring = true
        override.vm.box = 'digital_ocean'
        override.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
        override.nfs.functional = false
        provider.token = 'SOME_SECURE_KEY_IN_HERE_PROBS_WANT_TO_USE_ENV_VARS'
        provider.image = 'ubuntu-18-04-x64'
        provider.region = 'ams3'
        provider.size = 's-1vcpu-1gb'
      end

      config.vm.provision "shell", inline: $script
  end

end
