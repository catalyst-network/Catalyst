PIX = ((VI + SI + HI + AI + PI + DLI + DUI + WLI + WUI + MLI + MUI + NSI + NI + BI) / 14.0) x 10.0

VI = Protocol version index
	VI = 1 / r
        r = rank of node's protocol version
        Consensus protocol version is ranked 1 followed by the next most common protocol version

SI = Services index
    SI = 1 / r
        r = rank of node's services
        Consensus services is ranked 1 followed by the next most common services
        SI is set to 0.0 for node without NODE_NETWORK

HI = Height index
    HI = h / H
        h = node's latest height
        H = consensus height

AI = ASN index
    AI = ln((1 / n) x N) / ln(N)
        N = number of reachable nodes
        n = number of nodes from N with the same ASN

PI = Port index
    PI = 1.0 for 8333 (default port), 0.0 for other

DLI = Average daily latency index
    DLI = 1.0 for <=  300 ms
          0.9 for <=  400 ms
          0.8 for <=  500 ms
          0.7 for <=  600 ms
          0.6 for <=  700 ms
          0.5 for <=  800 ms
          0.4 for <=  900 ms
          0.3 for <= 1000 ms
          0.2 for <= 1100 ms
          0.1 for <= 1200 ms
          0.0 for  > 1200 ms

DUI = Daily uptime index
    DUI = t / T
        t = number of daily ticks with latency > 0
        T = number of daily ticks
        Tick with latency <= 0 indicates that node is unreachable

WLI = Average weekly latency index
    WLI = 1.0 for <=  300 ms
          0.9 for <=  400 ms
          0.8 for <=  500 ms
          0.7 for <=  600 ms
          0.6 for <=  700 ms
          0.5 for <=  800 ms
          0.4 for <=  900 ms
          0.3 for <= 1000 ms
          0.2 for <= 1100 ms
          0.1 for <= 1200 ms
          0.0 for  > 1200 ms

WUI = Weekly uptime index
    WUI = t / T
        t = number of weekly ticks with latency > 0
        T = number of weekly ticks
        Tick with latency <= 0 indicates that node is unreachable

MLI = Average monthly latency index
    MLI = 1.0 for <=  300 ms
          0.9 for <=  400 ms
          0.8 for <=  500 ms
          0.7 for <=  600 ms
          0.6 for <=  700 ms
          0.5 for <=  800 ms
          0.4 for <=  900 ms
          0.3 for <= 1000 ms
          0.2 for <= 1100 ms
          0.1 for <= 1200 ms
          0.0 for  > 1200 ms

MUI = Monthly uptime index
    MUI = t / T
        t = number of monthly ticks with latency > 0
        T = number of monthly ticks
        Tick with latency <= 0 indicates that node is unreachable

NSI = Network speed index
    NSI = 1.0 for ≈ 99th percentile
          0.9 for ≈ 90th percentile
          0.8 for ≈ 80th percentile
          0.7 for ≈ 70th percentile
          0.6 for ≈ 60th percentile
          0.5 for ≈ 50th percentile
          0.4 for ≈ 40th percentile
          0.3 for ≈ 30th percentile
          0.2 for ≈ 20th percentile
          0.1 for ≈ 10th percentile
          0.0 for < 10th percentile

NI = Nodes index
    NI = (p ∩ N) / N
        p = peers returned in addr responses
        N = reachable nodes
        NI >= 6σ is capped at 99th percentile

BI = Block index
    BI = 1.0 if valid block is returned in response to getdata request, 0.0 if otherwise