// TrainMeATC Analytics and Data Visualization
// Uses Plotly.js for charting and data export functionality

window.plotlyConfig = {
    responsive: true,
    displayModeBar: true,
    displaylogo: false,
    modeBarButtonsToRemove: ['pan2d', 'select2d', 'lasso2d', 'resetScale2d', 'zoom2d', 'zoomIn2d', 'zoomOut2d']
};

// Download file utility
window.downloadFile = function(filename, content, contentType) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Enhanced performance chart with aviation theme
window.createPerformanceChart = function(containerId, data, options = {}) {
    const defaultOptions = {
        title: 'Communication Performance Timeline',
        xAxisTitle: 'Turn Number',
        yAxisTitle: 'Score (%)',
        lineColor: '#1e3a8a',
        markerColor: '#0ea5e9',
        backgroundColor: 'rgba(0,0,0,0)'
    };
    
    const config = { ...defaultOptions, ...options };
    
    const trace = {
        x: data.x,
        y: data.y,
        mode: 'lines+markers',
        type: 'scatter',
        name: 'Performance Score',
        line: {
            color: config.lineColor,
            width: 3,
            shape: 'spline'
        },
        marker: {
            size: 8,
            color: config.markerColor,
            line: {
                color: config.lineColor,
                width: 2
            }
        },
        fill: 'tonexty',
        fillcolor: 'rgba(30, 58, 138, 0.1)'
    };
    
    // Add average line
    const avgScore = data.y.reduce((a, b) => a + b, 0) / data.y.length;
    const avgTrace = {
        x: [Math.min(...data.x), Math.max(...data.x)],
        y: [avgScore, avgScore],
        mode: 'lines',
        type: 'scatter',
        name: 'Average',
        line: {
            color: '#f59e0b',
            width: 2,
            dash: 'dash'
        },
        showlegend: false
    };
    
    const layout = {
        title: {
            text: config.title,
            font: { size: 18, family: 'Inter, sans-serif' }
        },
        xaxis: {
            title: config.xAxisTitle,
            gridcolor: 'rgba(128, 128, 128, 0.2)',
            zeroline: false
        },
        yaxis: {
            title: config.yAxisTitle,
            range: [0, 100],
            gridcolor: 'rgba(128, 128, 128, 0.2)',
            zeroline: false
        },
        plot_bgcolor: config.backgroundColor,
        paper_bgcolor: config.backgroundColor,
        font: { family: 'Inter, sans-serif' },
        margin: { t: 50, r: 30, b: 50, l: 60 },
        hovermode: 'x unified',
        showlegend: false
    };
    
    Plotly.newPlot(containerId, [trace, avgTrace], layout, window.plotlyConfig);
};

// Create scenario performance comparison chart
window.createScenarioComparisonChart = function(containerId, scenarios) {
    const trace = {
        x: scenarios.map(s => s.name),
        y: scenarios.map(s => s.averageScore),
        type: 'bar',
        marker: {
            color: scenarios.map(s => s.averageScore >= 80 ? '#10b981' : s.averageScore >= 60 ? '#f59e0b' : '#ef4444'),
            line: {
                color: '#1e3a8a',
                width: 1
            }
        },
        text: scenarios.map(s => `${s.averageScore.toFixed(1)}%`),
        textposition: 'auto'
    };
    
    const layout = {
        title: {
            text: 'Performance by Scenario Type',
            font: { size: 18, family: 'Inter, sans-serif' }
        },
        xaxis: {
            title: 'Scenario',
            tickangle: -45
        },
        yaxis: {
            title: 'Average Score (%)',
            range: [0, 100]
        },
        plot_bgcolor: 'rgba(0,0,0,0)',
        paper_bgcolor: 'rgba(0,0,0,0)',
        font: { family: 'Inter, sans-serif' },
        margin: { t: 50, r: 30, b: 100, l: 60 }
    };
    
    Plotly.newPlot(containerId, [trace], layout, window.plotlyConfig);
};

// Create communication timeline tree view
window.createTimelineTree = function(containerId, turns) {
    const nodes = turns.map((turn, index) => ({
        id: `turn-${index}`,
        label: `Turn ${turn.idx}`,
        score: turn.score,
        transcript: turn.transcript,
        verdict: turn.verdict,
        level: 0
    }));
    
    // This would integrate with a tree visualization library like D3.js
    // For now, we'll create a simple hierarchical view
    const container = document.getElementById(containerId);
    if (!container) return;
    
    container.innerHTML = `
        <div class="timeline-tree">
            ${nodes.map(node => `
                <div class="tree-node" data-score="${node.score}">
                    <div class="node-header">
                        <span class="node-label">${node.label}</span>
                        <span class="node-score badge ${getScoreBadgeClass(node.score)}">${(node.score * 100).toFixed(0)}%</span>
                    </div>
                    <div class="node-content">
                        <div class="transcript">${node.transcript || 'No transcript'}</div>
                        <div class="verdict text-muted">${node.verdict || 'No verdict'}</div>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
};

function getScoreBadgeClass(score) {
    if (score >= 0.8) return 'bg-success';
    if (score >= 0.6) return 'bg-warning';
    return 'bg-danger';
}

// Export chart as image
window.exportChartImage = function(containerId, filename = 'chart.png') {
    Plotly.toImage(containerId, {
        format: 'png',
        width: 1200,
        height: 600
    }).then(function(dataUrl) {
        const link = document.createElement('a');
        link.href = dataUrl;
        link.download = filename;
        link.click();
    });
};

// Dark theme support for charts
window.updateChartTheme = function(containerId, isDark) {
    const update = {
        'plot_bgcolor': isDark ? '#1e293b' : 'rgba(0,0,0,0)',
        'paper_bgcolor': isDark ? '#0f172a' : 'rgba(0,0,0,0)',
        'font.color': isDark ? '#f1f5f9' : '#1e293b',
        'xaxis.gridcolor': isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(128, 128, 128, 0.2)',
        'yaxis.gridcolor': isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(128, 128, 128, 0.2)'
    };
    
    Plotly.update(containerId, {}, update);
};